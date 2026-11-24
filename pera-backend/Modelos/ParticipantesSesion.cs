using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class ParticipantesSesion
{
    public long IdParticipacion { get; set; }

    public long IdSesion { get; set; }

    public long IdAsignacion { get; set; }

    public virtual Asistencia? Asistencia { get; set; }

    public virtual ICollection<BitacorasAvance> BitacorasAvances { get; set; } = new List<BitacorasAvance>();

    public virtual AsignacionesTutorium IdAsignacionNavigation { get; set; } = null!;

    public virtual SesionesTutorium IdSesionNavigation { get; set; } = null!;

    public virtual SolicitudesReprogramacion? SolicitudesReprogramacion { get; set; }
}
