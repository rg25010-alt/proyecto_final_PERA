using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Docente
{
    public long IdDocente { get; set; }

    public long IdUsuario { get; set; }

    public int IdDepartamento { get; set; }

    public string CodigoDocente { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsignacionDocente> AsignacionDocentes { get; set; } = new List<AsignacionDocente>();

    public virtual ICollection<Asistencia> Asistencia { get; set; } = new List<Asistencia>();

    public virtual ICollection<BitacorasAvance> BitacorasAvances { get; set; } = new List<BitacorasAvance>();

    public virtual Departamento IdDepartamentoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<SesionDocente> SesionDocentes { get; set; } = new List<SesionDocente>();

    public virtual ICollection<SesionesTutorium> SesionesTutoria { get; set; } = new List<SesionesTutorium>();
}
