using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class Usuario
{
    public long IdUsuario { get; set; }

    public short IdRol { get; set; }

    public int? IdDepartamento { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string ClaveHash { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<AsignacionesTutorium> AsignacionesTutoria { get; set; } = new List<AsignacionesTutorium>();

    public virtual ICollection<AuditoriaEvento> AuditoriaEventos { get; set; } = new List<AuditoriaEvento>();

    public virtual Docente? Docente { get; set; }

    public virtual Estudiante? EstudianteIdUsuarioNavigation { get; set; }

    public virtual ICollection<Estudiante> EstudianteIdUsuarioRegistraNavigations { get; set; } = new List<Estudiante>();

    public virtual ICollection<HabilitacionesEgreso> HabilitacionesEgresos { get; set; } = new List<HabilitacionesEgreso>();

    public virtual Departamento? IdDepartamentoNavigation { get; set; }

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<ParametrosPera> ParametrosPeras { get; set; } = new List<ParametrosPera>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<SolicitudesReprogramacion> SolicitudesReprogramacions { get; set; } = new List<SolicitudesReprogramacion>();
}
