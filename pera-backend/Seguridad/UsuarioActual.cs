using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;

namespace Pera.Backend.Seguridad;

public static class Roles
{
    public const string Administracion = "ADMIN_ACADEMICA";
    public const string Coordinacion = "COORDINACION_PERA";
    public const string Jefatura = "JEFATURA";
    public const string Docente = "DOCENTE";
    public const string Estudiante = "ESTUDIANTE";
}

public sealed record PerfilActual(long IdUsuario, string Rol, int? IdDepartamento);

public sealed class UsuarioActual(PeraDbContext db, IHttpContextAccessor http)
{
    public async Task<PerfilActual> ObtenerAsync(CancellationToken ct)
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            !long.TryParse(principal.FindFirst("sub")?.Value, out var id))
            throw AccesoDenegado();

        var rol = principal.FindFirst("role")?.Value;
        var perfil = await db.Usuarios.AsNoTracking()
            .Where(u => u.IdUsuario == id && u.Activo && u.IdRolNavigation.Codigo == rol)
            .Select(u => new PerfilActual(u.IdUsuario, u.IdRolNavigation.Codigo,
                u.IdRolNavigation.Codigo == Roles.Docente
                    ? (u.Docente != null && u.Docente.Activo ? (int?)u.Docente.IdDepartamento : null)
                    : u.IdDepartamento))
            .SingleOrDefaultAsync(ct);
        return perfil ?? throw AccesoDenegado();
    }

    private static GraphQLException AccesoDenegado() => new(ErrorBuilder.New()
        .SetMessage("Acceso denegado.").SetCode("AUTH_NOT_AUTHORIZED").Build());
}
