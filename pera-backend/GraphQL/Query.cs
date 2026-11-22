using Microsoft.EntityFrameworkCore;
using Pera.Backend.Datos;
using Pera.Backend.Modelos;

namespace Pera.Backend.GraphQL;

public class Query
{
    public async Task<List<Departamento>> GetDepartamentos(PeraDbContext db)
    {
        return await db.Departamentos.AsNoTracking().ToListAsync();
    }
}