using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;
using HotChocolate.Authorization;
using Pera.Backend.Seguridad;

namespace Pera.Backend.GraphQL;

public sealed record DepartamentoResumen(int IdDepartamento, string Nombre, bool Activo);

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
}
