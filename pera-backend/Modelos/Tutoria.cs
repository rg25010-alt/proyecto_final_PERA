namespace Pera.Backend.Modelos;

public sealed record Tutor(int Id, string Nombre, string Departamento, bool Activo);
public sealed record SolicitudAsignacion(int? TutorId, string? AreaRefuerzo);
public sealed record AsignacionTutor(
    int Id, string Carnet, Tutor Tutor, string AreaRefuerzo, DateTimeOffset FechaAsignacion, string Responsable);
