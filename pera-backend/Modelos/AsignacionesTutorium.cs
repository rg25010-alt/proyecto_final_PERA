using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class AsignacionesTutorium
{
    public long IdAsignacion { get; set; }

    public string CarnetEstudiante { get; set; } = null!;

    public long? IdUsuarioAsignador { get; set; }

    public string PeriodoAcademico { get; set; } = null!;

    public DateOnly FechaReferencia { get; set; }

    public short NumeroIntento { get; set; }

    public decimal? NotaFinal { get; set; }

    public string EstadoPera { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual ICollection<AsignacionDocente> AsignacionDocentes { get; set; } = new List<AsignacionDocente>();

    public virtual Estudiante CarnetEstudianteNavigation { get; set; } = null!;

    public virtual HabilitacionesEgreso? HabilitacionesEgreso { get; set; }

    public virtual Usuario? IdUsuarioAsignadorNavigation { get; set; }

    public virtual ICollection<ParticipantesSesion> ParticipantesSesions { get; set; } = new List<ParticipantesSesion>();
}
