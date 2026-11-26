namespace Pera.Backend.GraphQL;

// Fase 3 - Ítem 8: asignaciones y programación de tutorías.
public sealed record TutorAsignarInput(long IdDocente, string AreaRefuerzo);

public sealed record AsignarTutoresInput(
    string Carnet, string PeriodoAcademico, TutorAsignarInput[] Tutores);

public sealed record AsignacionTutoriaCreada(
    long IdAsignacion, string CarnetEstudiante, string PeriodoAcademico,
    short NumeroIntento, string EstadoPera, int CantidadTutores);

public sealed record AsignacionTutoriaResumen(
    long IdAsignacion, string CarnetEstudiante, string PeriodoAcademico,
    short NumeroIntento, decimal? NotaFinal, string EstadoPera, int CantidadTutores);

public sealed record TutorAsignadoResumen(
    long IdAsignacion, long IdDocente, string Nombres, string Apellidos,
    string Departamento, string AreaRefuerzo, bool Activo, DateOnly FechaAsignacion);

public sealed record CerrarAsignacionInput(long IdAsignacion, decimal NotaFinal);

public sealed record AsignacionCerrada(long IdAsignacion, decimal NotaFinal, string EstadoPera);

public sealed record ProgramarSesionInput(
    long IdAsignacion, DateTime FechaInicio, DateTime FechaFin, string Modalidad,
    string? UbicacionEnlace, long IdDocenteResponsable,
    long[] IdDocentes, long[]? IdAsignacionesParticipantes);

public sealed record SesionProgramada(
    long IdSesion, DateTime FechaInicio, DateTime FechaFin, string Modalidad,
    string EstadoSesion, int CantidadDocentes, int CantidadParticipantes);

public sealed record SesionPublicada(long IdSesion, string EstadoSesion);

public sealed record SesionTutoriaResumen(
    long IdSesion, DateTime FechaInicio, DateTime FechaFin, string Modalidad,
    string EstadoSesion, int CantidadDocentes, int CantidadParticipantes);

// Fase 3 - Ítem 9: seguimiento y notificaciones.
public sealed record RegistrarAsistenciaInput(
    long IdParticipacion, bool Asistio, string? Observaciones);

public sealed record AsistenciaResumen(
    long IdParticipacion, string CarnetEstudiante, bool? Asistio, string? Observaciones);

public sealed record AsistenciaRegistrada(
    long IdAsistencia, long IdParticipacion, bool Asistio, DateTime FechaRegistro);

public sealed record PorcentajeAsistencia(
    long IdSesion, int TotalParticipantes, int ConAsistencia, int Asistieron, decimal Porcentaje);

public sealed record RegistrarBitacoraInput(
    long IdParticipacion, string Descripcion, decimal? Calificacion, string? Acuerdos);

public sealed record BitacoraResumen(
    long IdBitacora, long IdParticipacion, string Descripcion,
    decimal? Calificacion, string? Acuerdos, DateTime FechaRegistro);

public sealed record BitacoraRegistrada(
    long IdBitacora, long IdParticipacion, string Descripcion,
    decimal? Calificacion, DateTime FechaRegistro);

public sealed record CrearNotificacionInput(
    string Tipo, string Mensaje, long? IdSesion, string? CarnetEstudiante,
    long? IdUsuarioDestino, string? CorreoDestino, DateTime? FechaProgramada);

public sealed record NotificacionResumen(
    long IdNotificacion, string Tipo, string Mensaje, DateTime FechaProgramada,
    string EstadoEnvio, string? CarnetEstudiante, long? IdSesion);

public sealed record NotificacionCreada(
    long IdNotificacion, string Tipo, string EstadoEnvio, DateTime FechaProgramada);

public sealed record NotificacionEnviada(
    long IdNotificacion, string EstadoEnvio, DateTime FechaEnvio);
