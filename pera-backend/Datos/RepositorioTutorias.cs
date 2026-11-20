using Pera.Backend.Modelos;

namespace Pera.Backend.Datos;

public interface IRepositorioTutorias
{
    IReadOnlyList<Tutor> ListarActivos();
    IReadOnlyList<AsignacionTutor> Consultar(string carnet);
    AsignacionTutor? Asignar(string carnet, Tutor tutor, string area, string responsable);
}

public sealed class RepositorioTutoriasEnMemoria : IRepositorioTutorias
{
    private readonly Tutor[] docentes =
    [
        new(1, "Ana Morales", "Ingeniería y Arquitectura", true),
        new(2, "Luis Rivera", "Ingeniería y Arquitectura", true),
        new(3, "Rosa Pérez", "Ingeniería y Arquitectura", false)
    ];
    private readonly List<AsignacionTutor> asignaciones = [];
    private readonly object control = new();

    public IReadOnlyList<Tutor> ListarActivos() => docentes.Where(t => t.Activo).ToArray();

    public IReadOnlyList<AsignacionTutor> Consultar(string carnet)
    {
        lock (control)
            return asignaciones.Where(a => a.Carnet == carnet).ToArray();
    }

    public AsignacionTutor? Asignar(string carnet, Tutor tutor, string area, string responsable)
    {
        lock (control)
        {
            if (asignaciones.Any(a => a.Carnet == carnet && a.Tutor.Id == tutor.Id &&
                string.Equals(a.AreaRefuerzo, area, StringComparison.OrdinalIgnoreCase)))
                return null;

            var asignacion = new AsignacionTutor(asignaciones.Count + 1, carnet, tutor, area,
                DateTimeOffset.UtcNow, responsable);
            asignaciones.Add(asignacion);
            return asignacion;
        }
    }
}
