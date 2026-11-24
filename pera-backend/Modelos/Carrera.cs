using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Carrera
{
    public int IdCarrera { get; set; }

    public int IdDepartamento { get; set; }

    public string NombreCarrera { get; set; } = null!;

    public virtual ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();

    public virtual Departamento IdDepartamentoNavigation { get; set; } = null!;
}
