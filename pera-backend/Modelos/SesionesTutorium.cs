using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class SesionesTutorium
{
    public long IdSesion { get; set; }

    public long IdDocenteCreador { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string Modalidad { get; set; } = null!;

    public string? UbicacionEnlace { get; set; }

    public string EstadoSesion { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual Docente IdDocenteCreadorNavigation { get; set; } = null!;

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<ParticipantesSesion> ParticipantesSesions { get; set; } = new List<ParticipantesSesion>();

    public virtual SesionDocente? SesionDocente { get; set; }
}
