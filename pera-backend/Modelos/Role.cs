using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Role
{
    public short IdRol { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
