using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class SolicitudesReprogramacion
{
    public long IdSolicitud { get; set; }

    public long IdParticipacion { get; set; }

    public DateTime FechaPropuestaInicio { get; set; }

    public DateTime FechaPropuestaFin { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public long? IdUsuarioResuelve { get; set; }

    public string? Respuesta { get; set; }

    public DateTime SolicitadoEn { get; set; }

    public DateTime? ResueltoEn { get; set; }

    public virtual ParticipantesSesion IdParticipacionNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioResuelveNavigation { get; set; }
}
