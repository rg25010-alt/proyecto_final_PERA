namespace Pera.Backend.Modelos;

public sealed record Expediente(
    string Carnet,
    string Nombre,
    int CarreraId,
    decimal Cum,
    int? TopeUv,
    string EstadoPera);

public sealed record CrearExpedienteSolicitud(
    string? Carnet,
    string? Nombre,
    int? CarreraId,
    decimal? Cum);
