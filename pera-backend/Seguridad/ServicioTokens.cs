using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Pera.Backend.Seguridad;

public sealed class ServicioTokens(SymmetricSecurityKey clave, string emisor, string audiencia,
    TimeSpan vigenciaAcceso, TimeSpan vigenciaRenovacion)
{
    public (string Token, DateTime ExpiraEn) CrearAcceso(long idUsuario, string codigoRol)
    {
        var ahora = DateTime.UtcNow;
        var expira = ahora.Add(vigenciaAcceso);
        var jwt = new JwtSecurityToken(emisor, audiencia,
        [
            new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString(CultureInfo.InvariantCulture)),
            new Claim("role", codigoRol),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        ], ahora, expira, new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), jwt.ValidTo);
    }

    public (string Token, string Hash, DateTime ExpiraEn) CrearRenovacion()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (token, HashRenovacion(token), DateTime.UtcNow.Add(vigenciaRenovacion));
    }

    public static string HashRenovacion(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
