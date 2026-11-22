using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Notificacione
{
    public long IdNotificacion { get; set; }

    public long? IdSesion { get; set; }

    public string? CarnetEstudiante { get; set; }

    public long? IdUsuarioDestino { get; set; }

    public string? CorreoDestino { get; set; }

    public string Tipo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public DateTime FechaProgramada { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public string EstadoEnvio { get; set; } = null!;

    public virtual Estudiante? CarnetEstudianteNavigation { get; set; }

    public virtual SesionesTutorium? IdSesionNavigation { get; set; }

    public virtual Usuario? IdUsuarioDestinoNavigation { get; set; }
}
