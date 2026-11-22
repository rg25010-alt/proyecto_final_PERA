using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Asistencia
{
    public long IdAsistencia { get; set; }

    public long IdParticipacion { get; set; }

    public long IdDocenteRegistra { get; set; }

    public bool Asistio { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? Observaciones { get; set; }

    public virtual Docente IdDocenteRegistraNavigation { get; set; } = null!;

    public virtual ParticipantesSesion IdParticipacionNavigation { get; set; } = null!;
}
