using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class CostosProducto
{
    public int Id { get; set; }

    public int? FkProducto { get; set; }

    public decimal? Costo { get; set; }
}
