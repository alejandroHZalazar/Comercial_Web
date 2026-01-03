using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class OrdenCompra
{
    public long Id { get; set; }

    public DateTime? Fecha { get; set; }

    public int? FkProveedor { get; set; }

    public decimal? Total { get; set; }

    public bool? Procesado { get; set; }

    public decimal? Iva { get; set; }

    public decimal? Recargo { get; set; }

    public decimal? Descuento { get; set; }
}
