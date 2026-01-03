using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class OrdenCompraDetalle
{
    public long Linea { get; set; }

    public long? FkOrdenCompra { get; set; }

    public int? FkProducto { get; set; }

    public string? CodBarras { get; set; }

    public string? CodProveedor { get; set; }

    public string? Descripcion { get; set; }

    public decimal? PrecioProveedor { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? Subtotal { get; set; }

    public bool? Procesado { get; set; }

    public decimal? CantRecibida { get; set; }

    public int FkColor { get; set; }
}
