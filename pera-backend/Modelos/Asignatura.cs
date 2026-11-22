using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Asignatura
{
    public int IdAsignatura { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public short UnidadesValorativas { get; set; }

    public virtual ICollection<Inscripcione> Inscripciones { get; set; } = new List<Inscripcione>();
}
