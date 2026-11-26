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
}
