using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class VentasDetalle
{
    public long Linea { get; set; }

    public long? FkVenta { get; set; }

    public int? FkProducto { get; set; }

    public string? CodBarras { get; set; }

    public string? CodProveedor { get; set; }

    public string? Descripcion { get; set; }

    public decimal? PrecioSinIva { get; set; }

    public decimal? PrecioConIva { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? Costo { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? Descuento { get; set; }

    public decimal? Recargo { get; set; }

    public decimal? SubtotalSinIva { get; set; }
}
