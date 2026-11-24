using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class BitacorasAvance
{
    public long IdBitacora { get; set; }

    public long IdParticipacion { get; set; }

    public long IdDocenteAutor { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal? Calificacion { get; set; }

    public string? Acuerdos { get; set; }

    public virtual Docente IdDocenteAutorNavigation { get; set; } = null!;

    public virtual ParticipantesSesion IdParticipacionNavigation { get; set; } = null!;
}
