using System.Collections.Concurrent;
using Pera.Backend.Modelos;

namespace Pera.Backend.Datos;

public interface IRepositorioExpedientes
{
    IReadOnlyList<Carrera> ListarCarreras();
    Expediente? BuscarPorCarnet(string carnet);
    bool Guardar(Expediente expediente);
}

public sealed class RepositorioExpedientesEnMemoria : IRepositorioExpedientes
{
    private readonly IReadOnlyList<Carrera> carreras = Array.AsReadOnly(new[]
    {
        new Carrera(1, "Ingeniería de Sistemas Informáticos"),
        new Carrera(2, "Ingeniería Civil")
    });

    private readonly ConcurrentDictionary<string, Expediente> expedientes =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Carrera> ListarCarreras() => carreras;

    public Expediente? BuscarPorCarnet(string carnet)
    {
        expedientes.TryGetValue(carnet, out var expediente);
        return expediente;
    }

    public bool Guardar(Expediente expediente)
    {
        return expedientes.TryAdd(expediente.Carnet, expediente);
    }
}
