using System;
using System.Collections.Generic;

namespace Pera.Backend.Modelos;

public partial class ReglasUv
{
    public int IdRegla { get; set; }

    public decimal CumMin { get; set; }

    public decimal CumMax { get; set; }

    public short UvMax { get; set; }

    public bool Vigente { get; set; }
}
