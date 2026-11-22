using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Estudiante
{
    public string Carnet { get; set; } = null!;

    public int IdCarrera { get; set; }

    public long IdUsuario { get; set; }

    public string? CorreoContacto { get; set; }

    public decimal CumActual { get; set; }

    public short IntentosFallidos { get; set; }

    public DateTime RegistradoEn { get; set; }

    public long IdUsuarioRegistra { get; set; }

    public virtual ICollection<AsignacionesTutorium> AsignacionesTutoria { get; set; } = new List<AsignacionesTutorium>();

    public virtual Carrera IdCarreraNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioRegistraNavigation { get; set; } = null!;

    public virtual ICollection<Inscripcione> Inscripciones { get; set; } = new List<Inscripcione>();

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();
}
