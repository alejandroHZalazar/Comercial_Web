namespace Domain.DTO;

// ── Request interno al service (sin SP, con LINQ) ────────────────────────────
public class DevolucionRequestDto
{
    public decimal Total      { get; set; }
    public decimal Costo      { get; set; }
    public int     FkCliente  { get; set; }
    public int     FkCajero   { get; set; }
    public decimal Iva        { get; set; }
    public decimal Descuento  { get; set; }
    public decimal Recargo    { get; set; }
    public int     FkVendedor { get; set; }
    public decimal Comision   { get; set; }
    public decimal Impuesto   { get; set; }
    public int     LlevaCC    { get; set; }
    public List<DevolucionFilaDto> Filas { get; set; } = new();
}

// ── Fila individual ──────────────────────────────────────────────────────────
public class DevolucionFilaDto
{
    public int     FkProducto    { get; set; }
    public decimal PrecioSinIva  { get; set; }
    public decimal PrecioConIva  { get; set; }
    public decimal Cantidad      { get; set; }
    public decimal DescRec       { get; set; }
    public decimal SubtotalSinIva { get; set; }
    public decimal Costo         { get; set; }
}

// ── Request completo desde la página web ─────────────────────────────────────
public class DevolverWebRequestDto
{
    public int                     FkCliente          { get; set; }
    public decimal                 Iva                { get; set; }
    public decimal                 Impuesto           { get; set; }
    public int                     FkVendedor         { get; set; }
    public decimal                 Comision           { get; set; }
    public decimal                 Total              { get; set; }
    // Descuento general (%) sobre el Total S/IVA — solo modo bonificacionesPorDetalle = 1.
    // Heredado de la venta original (editable). null/0 = sin descuento general.
    public decimal?                Descuento          { get; set; }
    public List<DevolucionFilaDto> Filas              { get; set; } = new();
    // Datos NC fiscal (opcionales)
    public int?    NroFacturaAsociada { get; set; }
    public string? FechaFacturaAsoc   { get; set; }   // "dd/MM/yyyy"
}

// ── Para impresión comprobante de devolución ──────────────────────────────────
public class DevolucionImpresionDto
{
    public long      DevolucionId       { get; set; }
    public DateTime? Fecha              { get; set; }
    public string?   NombreCliente      { get; set; }
    public string?   RazonSocial        { get; set; }
    public string?   Cuil               { get; set; }
    public string?   DireccionCliente   { get; set; }
    public string?   CondIvaAbrev       { get; set; }
    public string?   CondIvaDescripcion { get; set; }
    public decimal   Iva                { get; set; }
    public decimal?  Descuento          { get; set; }
    public decimal?  Recargo            { get; set; }
    public decimal   Impuesto           { get; set; }
    public decimal   TotalDevolucion    { get; set; }
    public List<DevolucionDetalleImpresionItemDto> Detalle { get; set; } = new();
}

// ── Reporte de devoluciones ──────────────────────────────────────────────────
public class DevolucionReporteItemDto
{
    public int       Id              { get; set; }
    public DateTime? Fecha           { get; set; }
    public int?      FkCliente       { get; set; }
    public string?   NombreCliente   { get; set; }
    public decimal   Iva             { get; set; }
    public decimal?  Descuento       { get; set; }
    public decimal?  Recargo         { get; set; }
    public decimal   Impuesto        { get; set; }
    public decimal   TotalDevolucion { get; set; }
    // Nota de Crédito asociada (si existe)
    public bool      TieneNC         { get; set; }
    public string?   NumeroNC        { get; set; }  // "PPPP-NNNNNNNN"
    public string?   LinkPdfNC       { get; set; }
}

public class DevolucionDetalleImpresionItemDto
{
    public int      FkProducto     { get; set; }
    public string?  Descripcion    { get; set; }
    public string?  CodBarras      { get; set; }
    public string?  CodProveedor   { get; set; }
    public decimal  Cantidad       { get; set; }
    public decimal  PrecioSinIva   { get; set; }
    public decimal  PrecioConIva   { get; set; }
    public decimal? Descuento      { get; set; }
    public decimal? Recargo        { get; set; }
    public decimal  SubtotalSinIva { get; set; }
}
