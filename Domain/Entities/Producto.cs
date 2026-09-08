using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Producto
{
    public int Id { get; set; }

    public string? CodProveedor { get; set; }

    public string? CodBarras { get; set; }

    public int? FkRubro { get; set; }

    public int? Iva { get; set; }

    public string? Descripcion { get; set; }

    public int? FkProveedor { get; set; }

    public bool? Baja { get; set; }

    public bool? Fraccionado { get; set; }

    public bool? Dolarizado { get; set; }

    public bool? EsPromocion { get; set; }

    // Porcentajes por producto — solo se usan cuando el proveedor tiene preciosPorProducto = 1.
    // Columnas decimal(18,2) DEFAULT NULL. NULL = el producto usa los porcentajes del proveedor.
    public decimal? Ganancia { get; set; }

    public decimal? Descuento { get; set; }

    public decimal? CantidadMinimaVenta { get; set; }

    public string? DescripcionLarga { get; set; }

    public byte[]? Imagen { get; set; }
}
