namespace Pera.Backend.Seguridad;

public static class Claves
{
    public static bool Verificar(string claveIngresada, string hashGuardado) =>
        BCrypt.Net.BCrypt.Verify(claveIngresada, hashGuardado);

    public static string CrearHash(string clave) =>
        BCrypt.Net.BCrypt.HashPassword(clave, workFactor: 12);
}
