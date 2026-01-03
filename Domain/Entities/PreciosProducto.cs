using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class PreciosProducto
{
    public int Id { get; set; }

    public int? FkProducto { get; set; }

    public decimal? Precio { get; set; }
}
