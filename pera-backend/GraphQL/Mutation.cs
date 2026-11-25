using Pera.Backend.Seguridad;
using Pera.Backend.Modelos;
using Pera.Backend.Datos;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Pera.Backend.GraphQL;

public sealed record RegistrarEstudianteInput(
    string Carnet,
    int IdCarrera,
    string Nombres,
    string Apellidos,
    string Correo,
    string Clave,
    string? CorreoContacto,
    decimal CumActual
);

public sealed record EstudianteRegistrado(
    string Carnet,
    long IdUsuario,
    int IdCarrera,
    string Nombres,
    string Apellidos,
    string Correo,
    decimal CumActual
);

public sealed record RegistrarInscripcionInput(
    string CarnetEstudiante,
    int IdAsignatura,
    string PeriodoAcademico
);

public sealed record InscripcionRegistrada(
    long IdInscripcion,
    string CarnetEstudiante,
    int IdAsignatura,
    string PeriodoAcademico,
    DateOnly FechaInscripcion,
    string Estado,
    int UvAcumuladas,
    int UvMaxPermitidas
);



public class Mutation
{
    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura])]
public async Task<InscripcionRegistrada> RegistrarInscripcionAsync(
    RegistrarInscripcionInput input,
    PeraDbContext db,
    [Service] UsuarioActual usuarioActual,
    CancellationToken ct)
{
    var perfil = await usuarioActual.ObtenerAsync(ct);

    var carnet = input.CarnetEstudiante.Trim();
    var periodo = input.PeriodoAcademico.Trim();

    if (string.IsNullOrWhiteSpace(carnet))
        throw new GraphQLException("El carnet es obligatorio.");

    if (string.IsNullOrWhiteSpace(periodo))
        throw new GraphQLException("El período académico es obligatorio.");

    var estudiante = await db.Estudiantes
        .AsNoTracking()
        .Include(e => e.IdCarreraNavigation)
        .SingleOrDefaultAsync(e => e.Carnet == carnet, ct);

    if (estudiante is null)
        throw new GraphQLException("El estudiante no existe.");

    if (perfil.Rol == Roles.Jefatura &&
        estudiante.IdCarreraNavigation.IdDepartamento != perfil.IdDepartamento)
    {
        throw new GraphQLException(
            "Jefatura solo puede inscribir estudiantes de su departamento.");
    }

    var asignatura = await db.Asignaturas
        .AsNoTracking()
        .SingleOrDefaultAsync(a => a.IdAsignatura == input.IdAsignatura, ct);

    if (asignatura is null)
        throw new GraphQLException("La asignatura no existe.");

    var regla = await db.ReglasUvs
        .AsNoTracking()
        .Where(r =>
            r.Vigente &&
            estudiante.CumActual >= r.CumMin &&
            estudiante.CumActual <= r.CumMax)
        .OrderBy(r => r.CumMin)
        .FirstOrDefaultAsync(ct);

    if (regla is null)
        throw new GraphQLException(
            "No existe una regla de UV vigente para el CUM del estudiante.");

    var yaExiste = await db.Inscripciones.AnyAsync(i =>
        i.CarnetEstudiante == carnet &&
        i.IdAsignatura == input.IdAsignatura &&
        i.PeriodoAcademico == periodo,
        ct);

    if (yaExiste)
        throw new GraphQLException(
            "El estudiante ya tiene registrada esa asignatura en ese período.");

    var uvActuales = await db.Inscripciones
        .Where(i =>
            i.CarnetEstudiante == carnet &&
            i.PeriodoAcademico == periodo &&
            i.Estado == "Activa")
        .SumAsync(i => (int?)i.IdAsignaturaNavigation.UnidadesValorativas, ct)
        ?? 0;

    var uvNuevas = uvActuales + asignatura.UnidadesValorativas;

    if (uvNuevas > regla.UvMax)
    {
        throw new GraphQLException(
            $"La inscripción supera el límite permitido. " +
            $"UV actuales: {uvActuales}, " +
            $"UV de la asignatura: {asignatura.UnidadesValorativas}, " +
            $"máximo permitido: {regla.UvMax}.");
    }

    await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

    var inscripcion = new Pera.Backend.Modelos.Inscripcione
    {
        CarnetEstudiante = carnet,
        IdAsignatura = input.IdAsignatura,
        PeriodoAcademico = periodo,
        FechaInscripcion = DateOnly.FromDateTime(DateTime.UtcNow),
        Estado = "Activa"
    };

    db.Inscripciones.Add(inscripcion);
    await db.SaveChangesAsync(ct);

    await tx.CommitAsync(ct);

    return new InscripcionRegistrada(
        inscripcion.IdInscripcion,
        inscripcion.CarnetEstudiante,
        inscripcion.IdAsignatura,
        inscripcion.PeriodoAcademico,
        inscripcion.FechaInscripcion,
        inscripcion.Estado,
        uvNuevas,
        regla.UvMax
    );
}

    [Authorize(Roles = [Roles.Jefatura])]
    public async Task<bool> CambiarEstadoDocenteAsync(long idDocente, bool activo,
        PeraDbContext db, [Service] UsuarioActual usuario, CancellationToken ct)
    {
        var perfil = await usuario.ObtenerAsync(ct);
        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);
        var filas = await db.Docentes
            .Where(d => d.IdDocente == idDocente && db.Usuarios.Any(u =>
                u.IdUsuario == perfil.IdUsuario && u.Activo && u.IdRolNavigation.Codigo == Roles.Jefatura &&
                u.IdDepartamento == d.IdDepartamento))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Activo, activo), ct);
        await tx.CommitAsync(ct);
        return filas == 1;
    }

[Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura])]
public async Task<EstudianteRegistrado> RegistrarEstudianteAsync(
    RegistrarEstudianteInput input,
    PeraDbContext db,
    [Service] UsuarioActual usuarioActual,
    CancellationToken ct)
{
    var perfil = await usuarioActual.ObtenerAsync(ct);

    var carnet = input.Carnet.Trim();
    var correo = input.Correo.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(carnet))
        throw new GraphQLException("El carnet es obligatorio.");

    if (string.IsNullOrWhiteSpace(input.Nombres) ||
        string.IsNullOrWhiteSpace(input.Apellidos))
        throw new GraphQLException("Los nombres y apellidos son obligatorios.");

    if (string.IsNullOrWhiteSpace(correo))
        throw new GraphQLException("El correo es obligatorio.");

    if (input.Clave.Length < 8)
        throw new GraphQLException("La contraseña debe tener al menos 8 caracteres.");

    if (input.CumActual < 0 || input.CumActual > 10)
        throw new GraphQLException("El CUM debe estar entre 0 y 10.");

    var carrera = await db.Carreras
        .AsNoTracking()
        .SingleOrDefaultAsync(c => c.IdCarrera == input.IdCarrera, ct);

    if (carrera is null)
        throw new GraphQLException("La carrera indicada no existe.");

    if (perfil.Rol == Roles.Jefatura &&
        carrera.IdDepartamento != perfil.IdDepartamento)
    {
        throw new GraphQLException(
            "Jefatura solo puede registrar estudiantes de su departamento.");
    }

    if (await db.Estudiantes.AnyAsync(e => e.Carnet == carnet, ct))
        throw new GraphQLException("Ya existe un estudiante con ese carnet.");

    if (await db.Usuarios.AnyAsync(u => u.Correo.ToLower() == correo, ct))
        throw new GraphQLException("Ya existe un usuario con ese correo.");

    var rolEstudiante = await db.Roles
        .SingleOrDefaultAsync(r => r.Codigo == Roles.Estudiante, ct);

    if (rolEstudiante is null)
        throw new GraphQLException("No se encontró el rol ESTUDIANTE.");

    await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

    var nuevoUsuario = new Usuario
    {
        IdRol = rolEstudiante.IdRol,
        IdDepartamento = null,
        Nombres = input.Nombres.Trim(),
        Apellidos = input.Apellidos.Trim(),
        Correo = correo,
        ClaveHash = Claves.CrearHash(input.Clave),
        Activo = true,
        CreadoEn = DateTime.UtcNow
    };

    db.Usuarios.Add(nuevoUsuario);
    await db.SaveChangesAsync(ct);

    var estudiante = new Estudiante
    {
        Carnet = carnet,
        IdCarrera = input.IdCarrera,
        IdUsuario = nuevoUsuario.IdUsuario,
        CorreoContacto = string.IsNullOrWhiteSpace(input.CorreoContacto)
            ? null
            : input.CorreoContacto.Trim(),
        CumActual = input.CumActual,
        IntentosFallidos = 0,
        RegistradoEn = DateTime.UtcNow,
        IdUsuarioRegistra = perfil.IdUsuario
    };

    db.Estudiantes.Add(estudiante);
    await db.SaveChangesAsync(ct);

    await tx.CommitAsync(ct);

    return new EstudianteRegistrado(
        estudiante.Carnet,
        nuevoUsuario.IdUsuario,
        estudiante.IdCarrera,
        nuevoUsuario.Nombres,
        nuevoUsuario.Apellidos,
        nuevoUsuario.Correo,
        estudiante.CumActual
    );
}
    public Task<bool> LogoutAsync(string token, [Service] ServicioAcceso acceso,
        CancellationToken cancellationToken) => acceso.CerrarSesionAsync(token, cancellationToken);

    public async Task<SesionEmitida> RefreshTokenAsync(string token,
        [Service] ServicioAcceso acceso, CancellationToken cancellationToken)
    {
        var sesion = await acceso.RenovarAsync(token, cancellationToken);
        return sesion ?? throw new GraphQLException(ErrorBuilder.New()
            .SetMessage("El token de renovación ya no es válido.")
            .SetCode("REFRESH_TOKEN_INVALIDO").Build());
    }

    public async Task<SesionEmitida> LoginAsync(string correo, string clave,
        [Service] ServicioAcceso acceso, CancellationToken cancellationToken)
    {
        var sesion = await acceso.IngresarAsync(correo, clave, cancellationToken);
        return sesion ?? throw new GraphQLException("Correo o contraseña incorrectos.");
    }
}
