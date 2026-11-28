using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;
using Pera.Backend.Modelos;

namespace Pera.Backend.Seguridad;

public sealed record SesionEmitida(string AccessToken, DateTime AccessTokenExpiraEn,
    string RefreshToken, DateTime RefreshTokenExpiraEn)
{
    public override string ToString() => nameof(SesionEmitida);
}

public sealed class ServicioAcceso(PeraDbContext db, ServicioTokens tokens)
{
    // Iguala el trabajo de BCrypt cuando no hay una cuenta que verificar.
    private static readonly string HashDeComparacion = BCrypt.Net.BCrypt.HashPassword(
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), workFactor: 12);

    public async Task<SesionEmitida?> IngresarAsync(string correo, string clave, CancellationToken ct)
    {
        var direccion = correo.Trim().ToLowerInvariant();
        if (direccion.Length is 0 or > 160 || string.IsNullOrEmpty(clave)) return null;

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Correo.ToLower() == direccion)
            .Select(u => new {
                u.IdUsuario, u.Activo, u.ClaveHash, Rol = u.IdRolNavigation.Codigo,
                u.Nombres, u.Apellidos
            })
            .SingleOrDefaultAsync(ct);

        var coincide = Claves.Verificar(clave, usuario?.ClaveHash ?? HashDeComparacion);
        if (!coincide || usuario is null || !usuario.Activo) return null;

        await using var tx = await db.IniciarAsync(usuario.IdUsuario, ct);
        var nombre = usuario.Nombres + " " + usuario.Apellidos;
        var sesion = await EmitirAsync(usuario.IdUsuario, usuario.Rol, nombre, null, ct);
        await tx.CommitAsync(ct);
        return sesion;
    }

    public async Task<SesionEmitida?> RenovarAsync(string token, CancellationToken ct)
    {
        if (token.Length != 43) return null; // 32 bytes en Base64URL sin relleno.

        var hash = ServicioTokens.HashRenovacion(token);
        var anterior = db.RefreshTokens.Where(t => t.TokenHash == hash);
        var actor = await ObtenerActorDelTokenAsync(hash, ct);
        if (actor is null) return null;
        await using var transaccion = await db.IniciarAsync(actor.Value, ct);
        // Solo una solicitud puede reclamar el token, incluso si llegan dos a la vez.
        var reclamados = await anterior
            .Where(t => t.RevocadoEn == null && t.ExpiraEn > DateTime.UtcNow && t.IdUsuarioNavigation.Activo)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevocadoEn, t => DateTime.UtcNow), ct);
        if (reclamados != 1) return null;

        var datos = await anterior.AsTracking()
            .Select(t => new {
                Token = t, Rol = t.IdUsuarioNavigation.IdRolNavigation.Codigo,
                t.IdUsuarioNavigation.Nombres, t.IdUsuarioNavigation.Apellidos
            })
            .SingleAsync(ct);
        var nombre = datos.Nombres + " " + datos.Apellidos;
        var sesion = await EmitirAsync(
            datos.Token.IdUsuario, datos.Rol, nombre, datos.Token, ct);
        await transaccion.CommitAsync(ct);
        return sesion;
    }

    public async Task<bool> CerrarSesionAsync(string token, CancellationToken ct)
    {
        if (token.Length != 43) return false;

        var hash = ServicioTokens.HashRenovacion(token);
        var actor = await ObtenerActorDelTokenAsync(hash, ct);
        if (actor is null) return false;
        await using var tx = await db.IniciarAsync(actor.Value, ct);
        var eventos = await db.Database.ExecuteSqlInterpolatedAsync($"""
            WITH sesion_cerrada AS (
                UPDATE refresh_tokens
                   SET revocado_en = now()
                 WHERE token_hash = {hash} AND revocado_en IS NULL AND expira_en > now()
                RETURNING id_usuario, id_refresh_token
            )
            INSERT INTO auditoria_eventos (id_usuario_actor, entidad, id_registro, accion)
            SELECT id_usuario, 'refresh_tokens', id_refresh_token::text, 'LOGOUT'
              FROM sesion_cerrada
            """, ct);
        await tx.CommitAsync(ct);
        return eventos == 1;
    }

    private Task<long?> ObtenerActorDelTokenAsync(string hash, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.TokenHash == hash && t.RevocadoEn == null && t.ExpiraEn > DateTime.UtcNow)
            .Select(t => (long?)t.IdUsuario).SingleOrDefaultAsync(ct);

    private async Task<SesionEmitida> EmitirAsync(long idUsuario, string rol,
        string nombre, RefreshToken? anterior, CancellationToken ct)
    {
        var acceso = tokens.CrearAcceso(idUsuario, rol, nombre);
        var renovacion = tokens.CrearRenovacion();
        var nuevo = new RefreshToken
        {
            IdUsuario = idUsuario,
            TokenHash = renovacion.Hash,
            ExpiraEn = renovacion.ExpiraEn
        };
        db.RefreshTokens.Add(nuevo);
        if (anterior is not null) anterior.ReemplazadoPorNavigation = nuevo;
        await db.SaveChangesAsync(ct);
        return new SesionEmitida(acceso.Token, acceso.ExpiraEn, renovacion.Token, renovacion.ExpiraEn);
    }
}
