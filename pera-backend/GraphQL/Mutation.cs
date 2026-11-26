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

    private static readonly string[] TiposNotificacion =
    [
        "Programacion", "Cambio", "Cancelacion", "ParticipanteAgregado",
        "TutorAgregado", "AlertaTemprana", "ResultadoPERA", "SolicitudReprogramacion"
    ];

    private static async Task<Docente?> DocenteAsync(PeraDbContext db, PerfilActual perfil,
        CancellationToken ct) =>
        await db.Docentes.AsNoTracking()
            .SingleOrDefaultAsync(d => d.IdUsuario == perfil.IdUsuario, ct);

    private static async Task<List<Docente>> DocentesAsync(PeraDbContext db,
        IEnumerable<long> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToArray();
        return await db.Docentes.AsNoTracking()
            .Where(d => idList.Contains(d.IdDocente))
            .ToListAsync(ct);
    }

    private static void ValidarDocentes(List<Docente> docentes, long[] ids,
        PerfilActual perfil)
    {
        if (docentes.Count != ids.Length)
            throw new GraphQLException("Uno o más docentes no existen.");

        if (docentes.Any(d => !d.Activo))
            throw new GraphQLException("Uno o más docentes están inactivos.");

        if (perfil.Rol is Roles.Jefatura &&
            docentes.Any(d => d.IdDepartamento != perfil.IdDepartamento))
        {
            throw new GraphQLException("Jefatura solo puede asignar docentes de su departamento.");
        }
    }

    private static async Task<long> DocenteAutorizadoEnSesionAsync(PeraDbContext db,
        PerfilActual perfil, long idSesion, CancellationToken ct)
    {
        var docente = await DocenteAsync(db, perfil, ct)
            ?? throw new GraphQLException("El usuario no tiene perfil de docente.");

        if (!docente.Activo)
            throw new GraphQLException("El docente está inactivo.");

        if (perfil.Rol is Roles.Jefatura && docente.IdDepartamento != perfil.IdDepartamento)
            throw new GraphQLException("Jefatura solo puede actuar en su departamento.");

        var autorizado = await db.SesionesTutoria.AnyAsync(s =>
            s.IdSesion == idSesion &&
            (s.IdDocenteCreador == docente.IdDocente ||
             db.SesionDocentes.Any(sd =>
                 sd.IdSesion == s.IdSesion && sd.IdDocente == docente.IdDocente)),
            ct);

        if (!autorizado)
            throw new GraphQLException("El docente no participa en la sesión indicada.");

        return docente.IdDocente;
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura])]
    public async Task<AsignacionTutoriaCreada> AsignarTutoresAsync(
        AsignarTutoresInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        var carnet = input.Carnet.Trim().ToUpperInvariant();
        var periodo = input.PeriodoAcademico.Trim();
        if (string.IsNullOrWhiteSpace(carnet) || string.IsNullOrWhiteSpace(periodo))
            throw new GraphQLException("El carnet y el período académico son obligatorios.");

        var tutores = input.Tutores ?? [];
        if (tutores.Length == 0)
            throw new GraphQLException("Debe asignar al menos un tutor.");

        if (tutores.Any(t => string.IsNullOrWhiteSpace(t.AreaRefuerzo)))
            throw new GraphQLException("El área de refuerzo es obligatoria para cada tutor.");

        var idsDocentes = tutores.Select(t => t.IdDocente).Distinct().ToArray();
        var docentes = await DocentesAsync(db, idsDocentes, ct);
        ValidarDocentes(docentes, idsDocentes, perfil);

        var estudiante = await db.Estudiantes.AsNoTracking()
            .Include(e => e.IdCarreraNavigation)
            .SingleOrDefaultAsync(e => e.Carnet == carnet, ct)
            ?? throw new GraphQLException("El estudiante no existe.");

        if (perfil.Rol is Roles.Jefatura &&
            estudiante.IdCarreraNavigation.IdDepartamento != perfil.IdDepartamento)
        {
            throw new GraphQLException(
                "Jefatura solo puede asignar tutores a estudiantes de su departamento.");
        }

        var enCurso = await db.AsignacionesTutoria.AnyAsync(a =>
            a.CarnetEstudiante == carnet &&
            a.PeriodoAcademico == periodo &&
            a.EstadoPera == "En Curso", ct);

        if (enCurso)
            throw new GraphQLException("El estudiante ya tiene un proceso PERA en curso en ese período.");

        var ultimoIntento = await db.AsignacionesTutoria
            .Where(a => a.CarnetEstudiante == carnet && a.PeriodoAcademico == periodo)
            .MaxAsync(a => (int?)a.NumeroIntento, ct) ?? 0;

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

        var ahora = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahora);
        var asignacion = new AsignacionesTutorium
        {
            CarnetEstudiante = carnet,
            IdUsuarioAsignador = perfil.IdUsuario,
            PeriodoAcademico = periodo,
            FechaReferencia = hoy,
            NumeroIntento = (short)(ultimoIntento + 1),
            EstadoPera = "En Curso",
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        db.AsignacionesTutoria.Add(asignacion);
        await db.SaveChangesAsync(ct);

        foreach (var tutor in tutores)
        {
            db.AsignacionDocentes.Add(new AsignacionDocente
            {
                IdAsignacion = asignacion.IdAsignacion,
                IdDocente = tutor.IdDocente,
                AreaRefuerzo = tutor.AreaRefuerzo.Trim(),
                Activo = true,
                FechaAsignacion = hoy
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new AsignacionTutoriaCreada(
            asignacion.IdAsignacion, carnet, periodo, asignacion.NumeroIntento,
            asignacion.EstadoPera, tutores.Length);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura])]
    public async Task<AsignacionCerrada> CerrarAsignacionAsync(
        CerrarAsignacionInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        if (input.NotaFinal < 0 || input.NotaFinal > 10)
            throw new GraphQLException("La nota final debe estar entre 0 y 10.");

        var asignacion = await db.AsignacionesTutoria
            .Include(a => a.CarnetEstudianteNavigation)
                .ThenInclude(e => e.IdCarreraNavigation)
            .SingleOrDefaultAsync(a => a.IdAsignacion == input.IdAsignacion, ct)
            ?? throw new GraphQLException("La asignación no existe.");

        if (asignacion.EstadoPera != "En Curso")
            throw new GraphQLException("La asignación ya fue cerrada.");

        if (perfil.Rol is Roles.Jefatura &&
            asignacion.CarnetEstudianteNavigation.IdCarreraNavigation.IdDepartamento
                != perfil.IdDepartamento)
        {
            throw new GraphQLException("Jefatura solo puede cerrar procesos de su departamento.");
        }

        var estado = input.NotaFinal >= 7m ? "Aprobado" : "Reprobado";

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);
        asignacion.NotaFinal = input.NotaFinal;
        asignacion.EstadoPera = estado;
        asignacion.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new AsignacionCerrada(asignacion.IdAsignacion, input.NotaFinal, estado);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
    public async Task<SesionProgramada> ProgramarSesionAsync(
        ProgramarSesionInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        if (input.FechaFin <= input.FechaInicio)
            throw new GraphQLException("La fecha de fin debe ser posterior a la de inicio.");

        var modalidad = input.Modalidad.Trim();
        if (modalidad is not ("Presencial" or "Virtual"))
            throw new GraphQLException("La modalidad debe ser Presencial o Virtual.");

        if (modalidad == "Virtual" && string.IsNullOrWhiteSpace(input.UbicacionEnlace))
            throw new GraphQLException("Las sesiones virtuales requieren un enlace.");

        var idsDocentes = input.IdDocentes?.Distinct().ToArray() ?? [];
        if (idsDocentes.Length == 0)
            throw new GraphQLException("Debe indicar al menos un docente.");

        if (!idsDocentes.Contains(input.IdDocenteResponsable))
            throw new GraphQLException("El docente responsable debe integrar la sesión.");

        var docentes = await DocentesAsync(db, idsDocentes, ct);
        ValidarDocentes(docentes, idsDocentes, perfil);

        if (perfil.Rol is Roles.Docente)
        {
            var propio = await DocenteAsync(db, perfil, ct);
            if (propio is null || input.IdDocenteResponsable != propio.IdDocente)
                throw new GraphQLException("Un docente solo puede programar sus propias sesiones.");
        }

        var idAsignaciones = (input.IdAsignacionesParticipantes ?? [])
            .Append(input.IdAsignacion)
            .Distinct()
            .ToArray();

        var asignaciones = await db.AsignacionesTutoria.AsNoTracking()
            .Where(a => idAsignaciones.Contains(a.IdAsignacion))
            .Select(a => new { a.IdAsignacion, a.EstadoPera })
            .ToListAsync(ct);

        if (asignaciones.Count != idAsignaciones.Length ||
            asignaciones.Any(a => a.EstadoPera != "En Curso"))
        {
            throw new GraphQLException(
                "Todas las asignaciones participantes deben existir y estar En Curso.");
        }

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

        var ahora = DateTime.UtcNow;
        var sesion = new SesionesTutorium
        {
            IdDocenteCreador = input.IdDocenteResponsable,
            FechaInicio = input.FechaInicio,
            FechaFin = input.FechaFin,
            Modalidad = modalidad,
            UbicacionEnlace = string.IsNullOrWhiteSpace(input.UbicacionEnlace)
                ? null
                : input.UbicacionEnlace.Trim(),
            EstadoSesion = "Borrador",
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        db.SesionesTutoria.Add(sesion);
        await db.SaveChangesAsync(ct);

        foreach (var idDocente in idsDocentes)
        {
            db.SesionDocentes.Add(new SesionDocente
            {
                IdSesion = sesion.IdSesion,
                IdDocente = idDocente,
                EsResponsable = idDocente == input.IdDocenteResponsable
            });
        }

        foreach (var idAsignacion in idAsignaciones)
        {
            db.ParticipantesSesions.Add(new ParticipantesSesion
            {
                IdSesion = sesion.IdSesion,
                IdAsignacion = idAsignacion
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new SesionProgramada(
            sesion.IdSesion, sesion.FechaInicio, sesion.FechaFin, sesion.Modalidad,
            sesion.EstadoSesion, idsDocentes.Length, idAsignaciones.Length);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
    public async Task<SesionPublicada> PublicarSesionAsync(long idSesion,
        PeraDbContext db, [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var sesion = await db.SesionesTutoria
            .SingleOrDefaultAsync(s => s.IdSesion == idSesion, ct)
            ?? throw new GraphQLException("La sesión no existe.");

        if (sesion.EstadoSesion != "Borrador")
            throw new GraphQLException("Solo se pueden publicar sesiones en estado Borrador.");

        if (perfil.Rol is Roles.Docente)
        {
            var propio = await DocenteAsync(db, perfil, ct);
            if (propio is null || sesion.IdDocenteCreador != propio.IdDocente)
                throw new GraphQLException("Un docente solo puede publicar sus propias sesiones.");
        }

        if (perfil.Rol is Roles.Jefatura)
        {
            var docente = await DocenteAsync(db, perfil, ct);
            if (docente is null || docente.IdDepartamento != perfil.IdDepartamento)
                throw new GraphQLException("Jefatura solo puede publicar sesiones de su departamento.");
        }

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);
        sesion.EstadoSesion = "Programada";
        sesion.ActualizadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new SesionPublicada(sesion.IdSesion, sesion.EstadoSesion);
    }

    [Authorize(Roles = [Roles.Docente, Roles.Jefatura])]
    public async Task<AsistenciaRegistrada> RegistrarAsistenciaAsync(
        RegistrarAsistenciaInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        var participante = await db.ParticipantesSesions
            .Include(p => p.Asistencia)
            .SingleOrDefaultAsync(p => p.IdParticipacion == input.IdParticipacion, ct)
            ?? throw new GraphQLException("El participante no existe.");

        var idDocente = await DocenteAutorizadoEnSesionAsync(
            db, perfil, participante.IdSesion, ct);

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

        Asistencia asistencia;
        if (participante.Asistencia is null)
        {
            asistencia = new Asistencia
            {
                IdParticipacion = participante.IdParticipacion,
                IdDocenteRegistra = idDocente,
                Asistio = input.Asistio,
                FechaRegistro = DateTime.UtcNow,
                Observaciones = string.IsNullOrWhiteSpace(input.Observaciones)
                    ? null
                    : input.Observaciones.Trim()
            };
            db.Asistencias.Add(asistencia);
        }
        else
        {
            asistencia = participante.Asistencia;
            asistencia.Asistio = input.Asistio;
            asistencia.FechaModificacion = DateTime.UtcNow;
            asistencia.Observaciones = string.IsNullOrWhiteSpace(input.Observaciones)
                ? null
                : input.Observaciones.Trim();
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new AsistenciaRegistrada(
            asistencia.IdAsistencia, participante.IdParticipacion,
            asistencia.Asistio, asistencia.FechaRegistro);
    }

    [Authorize(Roles = [Roles.Docente, Roles.Jefatura])]
    public async Task<BitacoraRegistrada> RegistrarBitacoraAsync(
        RegistrarBitacoraInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        var descripcion = input.Descripcion.Trim();
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new GraphQLException("La descripción es obligatoria.");

        if (input.Calificacion is < 0 or > 10)
            throw new GraphQLException("La calificación debe estar entre 0 y 10.");

        var participante = await db.ParticipantesSesions.AsNoTracking()
            .SingleOrDefaultAsync(p => p.IdParticipacion == input.IdParticipacion, ct)
            ?? throw new GraphQLException("El participante no existe.");

        var idDocente = await DocenteAutorizadoEnSesionAsync(
            db, perfil, participante.IdSesion, ct);

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

        var bitacora = new BitacorasAvance
        {
            IdParticipacion = participante.IdParticipacion,
            IdDocenteAutor = idDocente,
            Descripcion = descripcion,
            Calificacion = input.Calificacion,
            Acuerdos = string.IsNullOrWhiteSpace(input.Acuerdos)
                ? null
                : input.Acuerdos.Trim(),
            FechaRegistro = DateTime.UtcNow
        };

        db.BitacorasAvances.Add(bitacora);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BitacoraRegistrada(
            bitacora.IdBitacora, bitacora.IdParticipacion, bitacora.Descripcion,
            bitacora.Calificacion, bitacora.FechaRegistro);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
    public async Task<NotificacionCreada> CrearNotificacionAsync(
        CrearNotificacionInput input, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);

        var tipo = input.Tipo.Trim();
        var mensaje = input.Mensaje.Trim();
        if (!TiposNotificacion.Contains(tipo))
            throw new GraphQLException("El tipo de notificación no es válido.");

        if (string.IsNullOrWhiteSpace(mensaje))
            throw new GraphQLException("El mensaje es obligatorio.");

        var carnet = string.IsNullOrWhiteSpace(input.CarnetEstudiante)
            ? null
            : input.CarnetEstudiante.Trim().ToUpperInvariant();

        if (input.IdSesion is null && carnet is null)
            throw new GraphQLException("La notificación requiere una sesión o un estudiante.");

        long? idUsuarioDestino = input.IdUsuarioDestino;

        if (carnet is not null)
        {
            var estudiante = await db.Estudiantes.AsNoTracking()
                .SingleOrDefaultAsync(e => e.Carnet == carnet, ct)
                ?? throw new GraphQLException("El estudiante indicado no existe.");

            if (idUsuarioDestino is null &&
                string.IsNullOrWhiteSpace(input.CorreoDestino))
            {
                idUsuarioDestino = estudiante.IdUsuario;
            }
        }

        if (input.IdSesion is not null &&
            !await db.SesionesTutoria.AnyAsync(s => s.IdSesion == input.IdSesion.Value, ct))
        {
            throw new GraphQLException("La sesión indicada no existe.");
        }

        if (idUsuarioDestino is not null &&
            !await db.Usuarios.AnyAsync(u => u.IdUsuario == idUsuarioDestino.Value, ct))
        {
            throw new GraphQLException("El usuario destino no existe.");
        }

        if (idUsuarioDestino is null && string.IsNullOrWhiteSpace(input.CorreoDestino))
            throw new GraphQLException("Debe indicar un usuario o un correo de destino.");

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);

        var notificacion = new Notificacione
        {
            IdSesion = input.IdSesion,
            CarnetEstudiante = carnet,
            IdUsuarioDestino = idUsuarioDestino,
            CorreoDestino = string.IsNullOrWhiteSpace(input.CorreoDestino)
                ? null
                : input.CorreoDestino.Trim(),
            Tipo = tipo,
            Mensaje = mensaje,
            FechaProgramada = input.FechaProgramada ?? DateTime.UtcNow,
            EstadoEnvio = "Pendiente"
        };

        db.Notificaciones.Add(notificacion);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new NotificacionCreada(
            notificacion.IdNotificacion, notificacion.Tipo,
            notificacion.EstadoEnvio, notificacion.FechaProgramada);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion])]
    public async Task<NotificacionEnviada> MarcarNotificacionEnviadaAsync(
        long idNotificacion, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var notificacion = await db.Notificaciones
            .SingleOrDefaultAsync(n => n.IdNotificacion == idNotificacion, ct)
            ?? throw new GraphQLException("La notificación no existe.");

        if (notificacion.EstadoEnvio == "Enviado")
        {
            return new NotificacionEnviada(
                notificacion.IdNotificacion, notificacion.EstadoEnvio,
                notificacion.FechaEnvio ?? DateTime.UtcNow);
        }

        await using var tx = await db.IniciarAsync(perfil.IdUsuario, ct);
        notificacion.EstadoEnvio = "Enviado";
        notificacion.FechaEnvio = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new NotificacionEnviada(
            notificacion.IdNotificacion, notificacion.EstadoEnvio,
            notificacion.FechaEnvio!.Value);
    }
}
