using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class RefreshToken
{
    public long IdRefreshToken { get; set; }

    public long IdUsuario { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime ExpiraEn { get; set; }

    public DateTime? RevocadoEn { get; set; }

    public long? ReemplazadoPor { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<RefreshToken> InverseReemplazadoPorNavigation { get; set; } = new List<RefreshToken>();

    public virtual RefreshToken? ReemplazadoPorNavigation { get; set; }
}
