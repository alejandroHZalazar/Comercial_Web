using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class StockProducto
{
    public int Id { get; set; }

    public int? FkProducto { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? CantidadMinima { get; set; }
}
