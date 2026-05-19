namespace Domain.DTO;

// ── Autocomplete clientes ────────────────────────────────────────────────
public class PedidoClienteItem
{
    public int     ClienteId       { get; set; }
    public string? NombreComercial { get; set; }
    public string? Telefono        { get; set; }
    public string? Contacto        { get; set; }
    public string? DireccionFull   { get; set; }
}

// ── Autocomplete / búsqueda productos ───────────────────────────────────
public class PedidoProductoItem
{
    public int     ProductoId   { get; set; }
    public string? CodBarras    { get; set; }
    public string? CodProveedor { get; set; }
    public string? Descripcion  { get; set; }
    public decimal Stock        { get; set; }
    public decimal PrecioSinIva { get; set; }
    public decimal PrecioConIva { get; set; }
    public decimal PrecioOrig   { get; set; }
    public decimal Costo        { get; set; }
    public bool    Fraccionado  { get; set; }
    public bool    Dolarizado   { get; set; }
    public bool    EsPromocion  { get; set; }
}

// ── Búsqueda de pedidos existentes ───────────────────────────────────────
public class PedidoBuscarItem
{
    public int      Id             { get; set; }
    public DateTime? Fecha         { get; set; }
    public string?  NombreCliente  { get; set; }
    public int?     FkCliente      { get; set; }
    public string?  NombreVendedor { get; set; }
    public string?  Observacion    { get; set; }
    public decimal? Iva            { get; set; }
    public decimal? Total          { get; set; }
    public bool?    Impreso        { get; set; }
    public bool?    Vendido        { get; set; }
}

// ── Cabecera de pedido (para cargar pedido a editar) ─────────────────────
public class PedidoCabeceraDto
{
    public int      Id             { get; set; }
    public int?     FkCliente      { get; set; }
    public decimal? Iva            { get; set; }
    public int?     FkVendedor     { get; set; }
    public string?  Observacion    { get; set; }
    public decimal? Total          { get; set; }
    public DateTime? Fecha         { get; set; }
    public string?  NombreComercial { get; set; }
    public string?  Telefono       { get; set; }
    public string?  Contacto       { get; set; }
    public string?  DireccionFull  { get; set; }
    // Descuento/Recargo a nivel cabecera (nullable = no aplica regla global)
    public decimal? Descuento      { get; set; }
    public decimal? Recargo        { get; set; }

    // ── Ecommerce ──────────────────────────────────────────────────────────
    public int?     EsEcommerce           { get; set; }
    public string?  DireccionEntrega      { get; set; }
    public string?  DireccionEntregaTexto { get; set; }
    public decimal? CostoEnvio            { get; set; }
}

// ── Detalle de pedido para cargar en grilla ──────────────────────────────
public class PedidoDetalleItemDto
{
    public int     FkProducto    { get; set; }
    public string? CodBarras     { get; set; }
    public string? CodProveedor  { get; set; }
    public string? Descripcion   { get; set; }
    public string? Observ        { get; set; }
    public decimal Stock         { get; set; }
    public decimal PrecioSinIva  { get; set; }
    public decimal PrecioConIva  { get; set; }
    public decimal PrecioOrig    { get; set; }
    public decimal Costo         { get; set; }
    public decimal Cantidad      { get; set; }
    public decimal Descuento     { get; set; }
    public decimal Recargo       { get; set; }
    public decimal SubtotalSinIva { get; set; }
    public decimal Subtotal       { get; set; }   // total de línea c/IVA (preserva importe fraccionado)
    public bool    Fraccionado   { get; set; }
    public bool    Dolarizado    { get; set; }
}

// ── Request guardar pedido ───────────────────────────────────────────────
public class GuardarPedidoRequestDto
{
    public int     PedidoId    { get; set; }
    public int     FkCliente   { get; set; }
    public decimal Iva         { get; set; }
    public int     FkVendedor  { get; set; }
    public string? Observacion { get; set; }
    public decimal Total       { get; set; }
    public List<GuardarPedidoDetalleDto> Detalle { get; set; } = new();
}

public class GuardarPedidoDetalleDto
{
    public int     FkProducto    { get; set; }
    public string? CodBarras     { get; set; }
    public string? CodProveedor  { get; set; }
    public string? Descripcion   { get; set; }
    public decimal PrecioSinIva  { get; set; }
    public decimal Cantidad      { get; set; }
    public decimal PrecioOrig    { get; set; }
    public decimal Costo         { get; set; }
    public decimal PrecioConIva  { get; set; }
    public string? Observ        { get; set; }
    public decimal Descuento     { get; set; }
    public decimal Recargo       { get; set; }
    public decimal SubtotalSinIva { get; set; }
    public decimal Subtotal       { get; set; }   // total de línea c/IVA (preserva importe fraccionado)
}

// ── Crear cliente rápido desde el formulario de pedidos ──────────────────
public class CrearClienteRapidoDto
{
    public string  NombreComercial { get; set; } = "";
    public string? RazonSocial     { get; set; }
    public string? Cuil            { get; set; }
    public string? Direccion       { get; set; }
    public string? Email           { get; set; }
    public string? Telefono        { get; set; }
    public string? Celular         { get; set; }
    public string? Contacto        { get; set; }
    public int?    FkCondIva       { get; set; }
    public int?    FkVendedor      { get; set; }
    public int?    FkLocalidad     { get; set; }
    public int?    FkZona          { get; set; }
}
