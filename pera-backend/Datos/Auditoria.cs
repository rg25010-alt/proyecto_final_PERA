using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Pera.Backend.Datos;

public static class Auditoria
{
    public static async Task<IDbContextTransaction> IniciarAsync(
        this PeraDbContext db, long idActor, CancellationToken ct)
    {
        var transaccion = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var actor = idActor.ToString(CultureInfo.InvariantCulture);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.id_usuario', {actor}, true)", ct);
            return transaccion;
        }
        catch
        {
            await transaccion.DisposeAsync();
            throw;
        }
    }
}
