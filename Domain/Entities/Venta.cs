using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Venta
{
    public long Id { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal? TotalVenta { get; set; }

    public decimal? TotalCosto { get; set; }

    public int? FkCliente { get; set; }

    public int? FkCajero { get; set; }

    public string? Factura { get; set; }

    public decimal? Iva { get; set; }

    public DateOnly? FFactura { get; set; }

    public int? FkCondIva { get; set; }

    public decimal? Descuento { get; set; }

    public decimal? Recargo { get; set; }

    public int? FkVendedor { get; set; }

    public decimal? Comision { get; set; }

    public decimal? Impuesto { get; set; }
}
