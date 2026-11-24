using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class AsignacionDocente
{
    public long IdAsignacion { get; set; }

    public long IdDocente { get; set; }

    public string AreaRefuerzo { get; set; } = null!;

    public bool Activo { get; set; }

    public DateOnly FechaAsignacion { get; set; }

    public virtual AsignacionesTutorium IdAsignacionNavigation { get; set; } = null!;

    public virtual Docente IdDocenteNavigation { get; set; } = null!;
}
