using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;
using HotChocolate.Authorization;
using Pera.Backend.Seguridad;

namespace Pera.Backend.GraphQL;

public sealed record ReglaUvResumen(
    int IdRegla,
    decimal CumMin,
    decimal CumMax,
    short UvMax,
    bool Vigente
);

public sealed record AsignaturaResumen(
    int IdAsignatura,
    string Codigo,
    string Nombre,
    short UnidadesValorativas
);

public sealed record InscripcionResumen(
    long IdInscripcion,
    string CarnetEstudiante,
    string Estudiante,
    int IdAsignatura,
    string CodigoAsignatura,
    string Asignatura,
    short UnidadesValorativas,
    string PeriodoAcademico,
    DateOnly FechaInscripcion,
    string Estado
);
public sealed record DepartamentoResumen(int IdDepartamento, string Nombre, bool Activo);
public sealed record CarreraResumen(
    int IdCarrera,
    int IdDepartamento,
    string NombreCarrera
);

public sealed record UsuarioResumen(
    long IdUsuario,
    short IdRol,
    int? IdDepartamento,
    string Nombres,
    string Apellidos,
    string Correo,
    bool Activo
);

public sealed record DocenteResumen(
    long IdDocente,
    long IdUsuario,
    int IdDepartamento,
    string CodigoDocente,
    string Nombres,
    string Apellidos,
    string Correo,
    string Departamento,
    bool Activo
);

public sealed record EstudianteResumen(
    string Carnet,
    int IdCarrera,
    long IdUsuario,
    string Nombres,
    string Apellidos,
    string Correo,
    string? CorreoContacto,
    decimal CumActual,
    short IntentosFallidos,
    string Carrera,
    int IdDepartamento
);
public class Query
{
    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public Task<PerfilActual> MiPerfilAsync([Service] UsuarioActual usuario, CancellationToken ct)
        => usuario.ObtenerAsync(ct);

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
    public async Task<List<DepartamentoResumen>> GetDepartamentos(PeraDbContext db,
        [Service] UsuarioActual usuario, CancellationToken ct)
    {
        var perfil = await usuario.ObtenerAsync(ct);
        var consulta = db.Departamentos.AsNoTracking();
        if (perfil.Rol is Roles.Jefatura or Roles.Docente)
            consulta = consulta.Where(d => d.IdDepartamento == perfil.IdDepartamento);

        return await consulta.OrderBy(d => d.IdDepartamento)
            .Select(d => new DepartamentoResumen(d.IdDepartamento, d.Nombre, d.Activo)).ToListAsync(ct);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
public async Task<List<CarreraResumen>> GetCarreras(
    PeraDbContext db,
    [Service] UsuarioActual usuario,
    CancellationToken ct)
{
    var perfil = await usuario.ObtenerAsync(ct);

    var consulta = db.Carreras.AsNoTracking();

    if (perfil.Rol is Roles.Jefatura or Roles.Docente)
        consulta = consulta.Where(c => c.IdDepartamento == perfil.IdDepartamento);

    return await consulta
        .OrderBy(c => c.IdCarrera)
        .Select(c => new CarreraResumen(
            c.IdCarrera,
            c.IdDepartamento,
            c.NombreCarrera
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura])]
public async Task<List<UsuarioResumen>> GetUsuarios(
    PeraDbContext db,
    [Service] UsuarioActual usuario,
    CancellationToken ct)
{
    var perfil = await usuario.ObtenerAsync(ct);

    var consulta = db.Usuarios.AsNoTracking();

    if (perfil.Rol == Roles.Jefatura)
        consulta = consulta.Where(u => u.IdDepartamento == perfil.IdDepartamento);

    return await consulta
        .OrderBy(u => u.IdUsuario)
        .Select(u => new UsuarioResumen(
            u.IdUsuario,
            u.IdRol,
            u.IdDepartamento,
            u.Nombres,
            u.Apellidos,
            u.Correo,
            u.Activo
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
public async Task<List<DocenteResumen>> GetDocentes(
    PeraDbContext db,
    [Service] UsuarioActual usuario,
    CancellationToken ct)
{
    var perfil = await usuario.ObtenerAsync(ct);

    var consulta = db.Docentes
        .AsNoTracking()
        .Include(d => d.IdUsuarioNavigation)
        .Include(d => d.IdDepartamentoNavigation)
        .AsQueryable();

    if (perfil.Rol is Roles.Jefatura or Roles.Docente)
        consulta = consulta.Where(d => d.IdDepartamento == perfil.IdDepartamento);

    return await consulta
        .OrderBy(d => d.IdDocente)
        .Select(d => new DocenteResumen(
            d.IdDocente,
            d.IdUsuario,
            d.IdDepartamento,
            d.CodigoDocente,
            d.IdUsuarioNavigation.Nombres,
            d.IdUsuarioNavigation.Apellidos,
            d.IdUsuarioNavigation.Correo,
            d.IdDepartamentoNavigation.Nombre,
            d.Activo
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
public async Task<List<EstudianteResumen>> GetEstudiantes(
    PeraDbContext db,
    [Service] UsuarioActual usuario,
    CancellationToken ct)
{
    var perfil = await usuario.ObtenerAsync(ct);

    var consulta = db.Estudiantes
        .AsNoTracking()
        .AsQueryable();

    if (perfil.Rol is Roles.Jefatura or Roles.Docente)
    {
        consulta = consulta.Where(e =>
            e.IdCarreraNavigation.IdDepartamento == perfil.IdDepartamento);
    }

    return await consulta
        .OrderBy(e => e.Carnet)
        .Select(e => new EstudianteResumen(
            e.Carnet,
            e.IdCarrera,
            e.IdUsuario,
            e.IdUsuarioNavigation.Nombres,
            e.IdUsuarioNavigation.Apellidos,
            e.IdUsuarioNavigation.Correo,
            e.CorreoContacto,
            e.CumActual,
            e.IntentosFallidos,
            e.IdCarreraNavigation.NombreCarrera,
            e.IdCarreraNavigation.IdDepartamento
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [
    Roles.Administracion,
    Roles.Coordinacion,
    Roles.Jefatura,
    Roles.Docente,
    Roles.Estudiante
])]
public async Task<List<ReglaUvResumen>> GetReglasUv(
    PeraDbContext db,
    CancellationToken ct)
{
    return await db.ReglasUvs
        .AsNoTracking()
        .OrderBy(r => r.CumMin)
        .Select(r => new ReglaUvResumen(
            r.IdRegla,
            r.CumMin,
            r.CumMax,
            r.UvMax,
            r.Vigente
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [
    Roles.Administracion,
    Roles.Coordinacion,
    Roles.Jefatura,
    Roles.Docente,
    Roles.Estudiante
])]
public async Task<List<AsignaturaResumen>> GetAsignaturas(
    PeraDbContext db,
    CancellationToken ct)
{
    return await db.Asignaturas
        .AsNoTracking()
        .OrderBy(a => a.Codigo)
        .Select(a => new AsignaturaResumen(
            a.IdAsignatura,
            a.Codigo,
            a.Nombre,
            a.UnidadesValorativas
        ))
        .ToListAsync(ct);
}

[Authorize(Roles = [
    Roles.Administracion,
    Roles.Coordinacion,
    Roles.Jefatura,
    Roles.Docente,
    Roles.Estudiante
])]
public async Task<List<InscripcionResumen>> GetInscripciones(
    PeraDbContext db,
    [Service] UsuarioActual usuario,
    CancellationToken ct)
{
    var perfil = await usuario.ObtenerAsync(ct);

    var consulta = db.Inscripciones
        .AsNoTracking()
        .AsQueryable();

    if (perfil.Rol == Roles.Estudiante)
    {
        consulta = consulta.Where(i =>
            i.CarnetEstudianteNavigation.IdUsuario == perfil.IdUsuario);
    }

    if (perfil.Rol is Roles.Jefatura or Roles.Docente)
    {
        consulta = consulta.Where(i =>
            i.CarnetEstudianteNavigation.IdCarreraNavigation.IdDepartamento
            == perfil.IdDepartamento);
    }

    return await consulta
        .OrderByDescending(i => i.FechaInscripcion)
        .Select(i => new InscripcionResumen(
            i.IdInscripcion,
            i.CarnetEstudiante,
            i.CarnetEstudianteNavigation.IdUsuarioNavigation.Nombres
                + " "
                + i.CarnetEstudianteNavigation.IdUsuarioNavigation.Apellidos,
            i.IdAsignatura,
            i.IdAsignaturaNavigation.Codigo,
            i.IdAsignaturaNavigation.Nombre,
            i.IdAsignaturaNavigation.UnidadesValorativas,
            i.PeriodoAcademico,
            i.FechaInscripcion,
            i.Estado
        ))
        .ToListAsync(ct);
}

    private static async Task<long?> DocenteIdAsync(PeraDbContext db, PerfilActual perfil,
        CancellationToken ct) =>
        await db.Docentes.AsNoTracking()
            .Where(d => d.IdUsuario == perfil.IdUsuario)
            .Select(d => (long?)d.IdDocente)
            .SingleOrDefaultAsync(ct);

    private static async Task<string?> CarnetAsync(PeraDbContext db, PerfilActual perfil,
        CancellationToken ct) =>
        await db.Estudiantes.AsNoTracking()
            .Where(e => e.IdUsuario == perfil.IdUsuario)
            .Select(e => (string?)e.Carnet)
            .SingleOrDefaultAsync(ct);

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public async Task<List<AsignacionTutoriaResumen>> GetAsignacionesTutoriaAsync(
        string? carnet, string? periodo, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var consulta = db.AsignacionesTutoria.AsNoTracking().AsQueryable();

        if (perfil.Rol is Roles.Estudiante)
        {
            var carnetPropio = await CarnetAsync(db, perfil, ct);
            consulta = consulta.Where(a => a.CarnetEstudiante == carnetPropio);
        }
        else if (!string.IsNullOrWhiteSpace(carnet))
        {
            var filtro = carnet.Trim().ToUpperInvariant();
            consulta = consulta.Where(a => a.CarnetEstudiante == filtro);
        }

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            var filtroPeriodo = periodo.Trim();
            consulta = consulta.Where(a => a.PeriodoAcademico == filtroPeriodo);
        }

        if (perfil.Rol is Roles.Jefatura)
            consulta = consulta.Where(a =>
                a.CarnetEstudianteNavigation.IdCarreraNavigation.IdDepartamento == perfil.IdDepartamento);

        return await consulta
            .OrderByDescending(a => a.FechaReferencia)
            .Select(a => new AsignacionTutoriaResumen(
                a.IdAsignacion, a.CarnetEstudiante, a.PeriodoAcademico, a.NumeroIntento,
                a.NotaFinal, a.EstadoPera, a.AsignacionDocentes.Count))
            .ToListAsync(ct);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public async Task<List<SesionTutoriaResumen>> GetSesionesTutoriaAsync(
        string? estado, DateTime? desde, DateTime? hasta, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var consulta = db.SesionesTutoria.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var filtro = estado.Trim();
            consulta = consulta.Where(s => s.EstadoSesion == filtro);
        }
        if (desde is not null) consulta = consulta.Where(s => s.FechaInicio >= desde.Value);
        if (hasta is not null) consulta = consulta.Where(s => s.FechaInicio <= hasta.Value);

        if (perfil.Rol is Roles.Estudiante)
        {
            var carnetPropio = await CarnetAsync(db, perfil, ct);
            consulta = consulta.Where(s => s.ParticipantesSesions.Any(p =>
                p.IdAsignacionNavigation.CarnetEstudiante == carnetPropio));
        }
        else if (perfil.Rol is Roles.Docente)
        {
            var idDocente = await DocenteIdAsync(db, perfil, ct);
            consulta = consulta.Where(s => s.IdDocenteCreador == idDocente ||
                db.SesionDocentes.Any(sd => sd.IdSesion == s.IdSesion && sd.IdDocente == idDocente));
        }

        return await consulta
            .OrderBy(s => s.FechaInicio)
            .Select(s => new SesionTutoriaResumen(
                s.IdSesion, s.FechaInicio, s.FechaFin, s.Modalidad, s.EstadoSesion,
                db.SesionDocentes.Count(sd => sd.IdSesion == s.IdSesion),
                s.ParticipantesSesions.Count))
            .ToListAsync(ct);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public async Task<List<AsistenciaResumen>> GetAsistenciasAsync(long idSesion,
        PeraDbContext db, [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var consulta = db.ParticipantesSesions.AsNoTracking()
            .Where(p => p.IdSesion == idSesion);

        if (perfil.Rol is Roles.Estudiante)
        {
            var carnetPropio = await CarnetAsync(db, perfil, ct);
            consulta = consulta.Where(p => p.IdAsignacionNavigation.CarnetEstudiante == carnetPropio);
        }

        return await consulta
            .Select(p => new AsistenciaResumen(
                p.IdParticipacion,
                p.IdAsignacionNavigation.CarnetEstudiante,
                p.Asistencia == null ? null : p.Asistencia.Asistio,
                p.Asistencia == null ? null : p.Asistencia.Observaciones))
            .ToListAsync(ct);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente])]
    public async Task<PorcentajeAsistencia> GetPorcentajeAsistenciaAsync(long idSesion,
        PeraDbContext db, CancellationToken ct)
    {
        var datos = await db.ParticipantesSesions.AsNoTracking()
            .Where(p => p.IdSesion == idSesion)
            .Select(p => new
            {
                Asistio = p.Asistencia == null ? (bool?)null : p.Asistencia.Asistio
            })
            .ToListAsync(ct);

        var total = datos.Count;
        var conAsistencia = datos.Count(d => d.Asistio.HasValue);
        var asistieron = datos.Count(d => d.Asistio == true);
        var porcentaje = total == 0 ? 0m : Math.Round(asistieron * 100m / total, 2);

        return new PorcentajeAsistencia(idSesion, total, conAsistencia, asistieron, porcentaje);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public async Task<List<BitacoraResumen>> GetBitacorasAsync(long idParticipacion,
        PeraDbContext db, [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var consulta = db.BitacorasAvances.AsNoTracking()
            .Where(b => b.IdParticipacion == idParticipacion);

        if (perfil.Rol is Roles.Estudiante)
        {
            var carnetPropio = await CarnetAsync(db, perfil, ct);
            consulta = consulta.Where(b =>
                b.IdParticipacionNavigation.IdAsignacionNavigation.CarnetEstudiante == carnetPropio);
        }

        return await consulta
            .OrderByDescending(b => b.FechaRegistro)
            .Select(b => new BitacoraResumen(
                b.IdBitacora, b.IdParticipacion, b.Descripcion,
                b.Calificacion, b.Acuerdos, b.FechaRegistro))
            .ToListAsync(ct);
    }

    [Authorize(Roles = [Roles.Administracion, Roles.Coordinacion, Roles.Jefatura, Roles.Docente, Roles.Estudiante])]
    public async Task<List<NotificacionResumen>> GetNotificacionesAsync(
        string? tipo, string? estado, PeraDbContext db,
        [Service] UsuarioActual usuarioActual, CancellationToken ct)
    {
        var perfil = await usuarioActual.ObtenerAsync(ct);
        var consulta = db.Notificaciones.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            var filtroTipo = tipo.Trim();
            consulta = consulta.Where(n => n.Tipo == filtroTipo);
        }
        if (!string.IsNullOrWhiteSpace(estado))
        {
            var filtroEstado = estado.Trim();
            consulta = consulta.Where(n => n.EstadoEnvio == filtroEstado);
        }
        if (perfil.Rol is Roles.Estudiante)
        {
            var carnetPropio = await CarnetAsync(db, perfil, ct);
            consulta = consulta.Where(n => n.CarnetEstudiante == carnetPropio);
        }

        return await consulta
            .OrderByDescending(n => n.FechaProgramada)
            .Select(n => new NotificacionResumen(
                n.IdNotificacion, n.Tipo, n.Mensaje, n.FechaProgramada,
                n.EstadoEnvio, n.CarnetEstudiante, n.IdSesion))
            .ToListAsync(ct);
    }
}
