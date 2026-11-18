namespace Domain.DTO;

/// <summary>Fila del reporte "Listado de Stock en Existencia" (solo productos con stock &gt; 0).</summary>
public class StockExistenciaDto
{
    public int     ProductoId  { get; set; }
    public string  Codigo      { get; set; } = string.Empty;
    public string  Descripcion { get; set; } = string.Empty;
    public string  Rubro       { get; set; } = string.Empty;
    public decimal Stock       { get; set; }

    /// <summary>Precio de costo unitario (costosProductos.costo).</summary>
    public decimal Costo       { get; set; }

    /// <summary>Precio de lista unitario sin IVA (preciosProductos.precio).</summary>
    public decimal PrecioLista { get; set; }

    /// <summary>Stock × Costo, redondeado a 2 decimales.</summary>
    public decimal TotalCosto  { get; set; }

    /// <summary>Stock × Precio de lista, redondeado a 2 decimales.</summary>
    public decimal TotalVenta  { get; set; }

    /// <summary>TotalVenta − TotalCosto.</summary>
    public decimal Rentabilidad { get; set; }
}
