namespace Pera.Backend.Seguridad;

public static class Claves
{
    public static bool Verificar(string claveIngresada, string hashGuardado) =>
        BCrypt.Net.BCrypt.Verify(claveIngresada, hashGuardado);
}
