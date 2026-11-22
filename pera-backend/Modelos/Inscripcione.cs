using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Inscripcione
{
    public long IdInscripcion { get; set; }

    public string CarnetEstudiante { get; set; } = null!;

    public int IdAsignatura { get; set; }

    public string PeriodoAcademico { get; set; } = null!;

    public DateOnly FechaInscripcion { get; set; }

    public string Estado { get; set; } = null!;

    public virtual Estudiante CarnetEstudianteNavigation { get; set; } = null!;

    public virtual Asignatura IdAsignaturaNavigation { get; set; } = null!;
}
