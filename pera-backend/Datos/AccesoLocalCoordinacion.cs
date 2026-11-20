using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;

namespace Pera.Backend.Datos;

// Credencial de desarrollo emitida desde la consola local mientras se integra el acceso institucional.
public static class AccesoLocalCoordinacion
{
    public static string Emitir(IServiceProvider servicios)
    {
        var esquema = BearerTokenDefaults.AuthenticationScheme;
        var opciones = servicios.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(esquema);
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Environment.UserName),
            new Claim(ClaimTypes.Role, "CoordinacionPERA")
        ], esquema);
        var propiedades = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidad), propiedades, esquema);
        return opciones.BearerTokenProtector.Protect(ticket);
    }
}
