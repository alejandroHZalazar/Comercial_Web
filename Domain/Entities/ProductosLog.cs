using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ProductosLog
{
    public int Id { get; set; }

    public int? FkProducto { get; set; }

    public decimal? PrecioProv { get; set; }

    public decimal? PrecioLista { get; set; }

    public decimal? PrecioCosto { get; set; }

    public DateOnly? ModifDate { get; set; }
}
