using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class HabilitacionesEgreso
{
    public long IdHabilitacion { get; set; }

    public long IdAsignacion { get; set; }

    public long IdUsuarioJefatura { get; set; }

    public bool Habilitado { get; set; }

    public string? Observacion { get; set; }

    public DateTime RegistradoEn { get; set; }

    public virtual AsignacionesTutorium IdAsignacionNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioJefaturaNavigation { get; set; } = null!;
}
