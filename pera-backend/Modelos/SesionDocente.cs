using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class SesionDocente
{
    public long IdSesion { get; set; }

    public long IdDocente { get; set; }

    public bool EsResponsable { get; set; }

    public virtual Docente IdDocenteNavigation { get; set; } = null!;

    public virtual SesionesTutorium IdSesionNavigation { get; set; } = null!;
}
