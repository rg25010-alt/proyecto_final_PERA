using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Pera.Backend.Seguridad;

public static class ConfiguracionJwt
{
    public static void AgregarSeguridadPera(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var jwt = configuracion.GetRequiredSection("Jwt");
        var clave = jwt["Key"];
        var emisor = jwt["Issuer"];
        var audiencia = jwt["Audience"];
        var minutos = jwt.GetValue<int>("AccessTokenMinutes");
        var dias = jwt.GetValue<int>("RefreshTokenDays");

        if (string.IsNullOrWhiteSpace(clave) || clave.StartsWith('<') || Encoding.UTF8.GetByteCount(clave) < 32)
            throw new InvalidOperationException("Configurá Jwt:Key con una clave aleatoria de al menos 32 bytes.");
        if (string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(audiencia) || minutos <= 0 || dias <= 0)
            throw new InvalidOperationException("Revisá el emisor, la audiencia y las vigencias de Jwt.");

        var firma = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
        servicios.AddSingleton(new ServicioTokens(firma, emisor, audiencia,
            TimeSpan.FromMinutes(minutos), TimeSpan.FromDays(dias)));
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opciones =>
        {
            opciones.MapInboundClaims = false;
            opciones.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = firma,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateIssuer = true, ValidIssuer = emisor,
                ValidateAudience = true, ValidAudience = audiencia,
                RequireExpirationTime = true, ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub", RoleClaimType = "role"
            };
        });
    }
}
