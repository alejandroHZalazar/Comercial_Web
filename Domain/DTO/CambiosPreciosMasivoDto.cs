namespace Domain.DTO;

// ---------------------------------------------------------------
// Fila parseada del CSV
// ---------------------------------------------------------------
public class FilaCsvPrecioDto
{
    public string  Codigo      { get; set; } = string.Empty;
    public decimal Precio      { get; set; }
    public string  Descripcion { get; set; } = string.Empty;
    public string  CodBarras   { get; set; } = string.Empty;
    public int     IdProveedor { get; set; }
}

// ---------------------------------------------------------------
// Resultado de actualizar un precio
// ---------------------------------------------------------------
public class CambioPrecioResultDto
{
    public bool    Success      { get; set; }
    public bool    EsNuevo      { get; set; }   // true = no encontrado
    public string  Mensaje      { get; set; } = string.Empty;
    public int     ProductoId   { get; set; }
    public string  CodProveedor { get; set; } = string.Empty;
    public string  Descripcion  { get; set; } = string.Empty;
}

// ---------------------------------------------------------------
// Request para insertar un producto nuevo desde la grilla
// ---------------------------------------------------------------
public class InsertarProductoNuevoRequest
{
    public string  CodProveedor  { get; set; } = string.Empty;
    public string  CodBarras     { get; set; } = string.Empty;
    public string  Descripcion   { get; set; } = string.Empty;
    public int     IdProveedor   { get; set; }
    public decimal PrecioProv    { get; set; }
    public decimal StockInicial  { get; set; }
    public decimal CantMinima    { get; set; }
    // Porcentajes del CSV — solo se usan/persisten si el proveedor tiene preciosPorProducto = 1.
    // NULL = columna vacía (proveedor por-proveedor).
    public decimal? Ganancia     { get; set; }
    public decimal? Descuento    { get; set; }
}

public class InsertarProductoNuevoResult
{
    public bool   Success   { get; set; }
    public string Mensaje   { get; set; } = string.Empty;
    public int    ProductoId{ get; set; }
}
