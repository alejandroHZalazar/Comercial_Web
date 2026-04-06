namespace Domain.DTO;

// ----------------------------------------------------------------
// TAB 1 — Lotes de ingreso
// ----------------------------------------------------------------

/// <summary>Cabecera de un lote: agrupación por NroComprobante + FechaMov.</summary>
public class LoteIngresoDto
{
    public string    NroComprobante  { get; set; } = string.Empty;
    public DateTime? FechaMov        { get; set; }
    public string    ProveedorNombre { get; set; } = string.Empty;
    public int       CantProductos   { get; set; }
    public decimal   TotalCosto      { get; set; }
}

/// <summary>Detalle de un ítem dentro de un lote.</summary>
public class LoteIngresoDetalleDto
{
    public long      Id             { get; set; }
    public string    CodProveedor   { get; set; } = string.Empty;
    public string    Descripcion    { get; set; } = string.Empty;
    public DateOnly? FechaEntrega   { get; set; }
    public string    Tipo           { get; set; } = string.Empty;
    public decimal   StockAnt       { get; set; }
    public decimal   StockAct       { get; set; }
    public decimal   Cantidad       { get; set; }
    public decimal   Costo          { get; set; }
    public string    NroComprobante { get; set; } = string.Empty;
    public DateTime? FechaMov       { get; set; }
}

// ----------------------------------------------------------------
// TAB 2 — Movimientos por producto
// ----------------------------------------------------------------

/// <summary>Producto encontrado en el buscador del tab 2.</summary>
public class ProductoConMovimientosDto
{
    public int     ProductoId   { get; set; }
    public string  CodProveedor { get; set; } = string.Empty;
    public string  CodBarras    { get; set; } = string.Empty;
    public string  Descripcion  { get; set; } = string.Empty;
    public int     CantMov      { get; set; }
}

/// <summary>Fila de movimiento de un producto (equivale a la salida del SP sp_ProductosTraerMovimientos).</summary>
public class MovimientoItemDto
{
    public string    CodProveedor   { get; set; } = string.Empty;
    public string    Descripcion    { get; set; } = string.Empty;
    public DateTime? FechaMov       { get; set; }
    public string    Tipo           { get; set; } = string.Empty;
    public decimal   StockAnt       { get; set; }
    public decimal   Cantidad       { get; set; }   // negativo si tipoMov == 3 (baja)
    public decimal   StockAct       { get; set; }
    public string    NroComprobante { get; set; } = string.Empty;
}
