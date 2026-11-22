using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class AuditoriaEvento
{
    public long IdEvento { get; set; }

    public long? IdUsuarioActor { get; set; }

    public string Entidad { get; set; } = null!;

    public string? IdRegistro { get; set; }

    public string Accion { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime OcurridoEn { get; set; }

    public virtual Usuario? IdUsuarioActorNavigation { get; set; }
}
