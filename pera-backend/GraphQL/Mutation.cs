using Pera.Backend.Seguridad;
using Pera.Backend.Datos;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Pera.Backend.GraphQL;

public class Mutation
{
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
