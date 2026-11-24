using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class ParametrosPera
{
    public int IdParametro { get; set; }

    public string Codigo { get; set; } = null!;

    public decimal ValorNumerico { get; set; }

    public long? IdUsuarioActualiza { get; set; }

    public DateTime ActualizadoEn { get; set; }

    public virtual Usuario? IdUsuarioActualizaNavigation { get; set; }
}
