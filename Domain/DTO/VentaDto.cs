namespace Domain.DTO;

// ── Parámetros del sistema para ventas ──────────────────────────────────────
public class VentaParametrosDto
{
    public int     LlevaCC                { get; set; }
    public int     Comisiona              { get; set; }
    public int     TieneConsumidorFinal   { get; set; }
    public int     ClienteConsumidorFinal { get; set; }
    public int     TieneMediosPagos       { get; set; }
    public int     ImputaEnVenta          { get; set; }
    public int     TieneCaja              { get; set; }
    public int     FacturaEnVenta         { get; set; }
    public int     FacturaFiscal          { get; set; }
    public int     FacturaElectronica     { get; set; }
    public string  MarcaFiscal            { get; set; } = "";
    public decimal ValorDolar             { get; set; }
    public int     HaceNotaVentaTK        { get; set; }
    public int     AnchoTk                { get; set; }
    public int     PuntoVenta             { get; set; }
    public int     TieneLectoraCB         { get; set; }
    public int     FiltraPorProveedor     { get; set; }
    public int     BonificacionPorLinea   { get; set; }
    public int     DolarizaProductos      { get; set; }
    public int     Decimales              { get; set; }
    public int     DecimalesCant          { get; set; }
    public int     IndiceBusqueda         { get; set; }
    public int     IdPlanEfectivo         { get; set; }

    // Balanza
    public int     TieneProductosBalanza  { get; set; }
    public string  PrefijoBalanza         { get; set; } = "";
    public string  PosicionProducto       { get; set; } = "";
    public string  PosicionPrecio         { get; set; } = "";
    public string  DivisorPrecio          { get; set; } = "1";
}

// ── Datos del cliente para la venta ─────────────────────────────────────────
public class ClienteVentaDataDto
{
    public int     Id             { get; set; }
    public string? NombreComercial{ get; set; }
    public string? RazonSocial    { get; set; }
    public string? Cuil           { get; set; }
    public string? Email          { get; set; }
    public string? Telefono       { get; set; }
    public string? DireccionFull  { get; set; }
    public string? CondIvaAbrev   { get; set; }
    public string? CondIvaLetra   { get; set; }
    public string? CondIvaAbrevFE   { get; set; }
    public string? CondIvaDescripcion { get; set; }
    public string? Provincia      { get; set; }
}

// ── Request guardar venta ────────────────────────────────────────────────────
public class GrabarVentaRequestDto
{
    public decimal  Total            { get; set; }
    public decimal  Costo            { get; set; }
    public int      FkCliente        { get; set; }
    public int      FkCajero         { get; set; }
    public decimal  Iva              { get; set; }
    public decimal? Descuento        { get; set; }
    public decimal? Recargo          { get; set; }
    public int      FkVendedor       { get; set; }
    public decimal  Comision         { get; set; }
    public decimal  Impuesto         { get; set; }
    public bool     LlevaCC          { get; set; }
    public bool     ImputaEnVenta    { get; set; }
    public bool     TieneMediosPagos { get; set; }
    public decimal  ImporteCobro     { get; set; }
    public bool     HaceCaja         { get; set; }
    public int      CajaId           { get; set; }
    public int      PedidoCargado    { get; set; }   // 0 = no vino de pedido
    public List<GrabarVentaDetalleDto>   Detalle    { get; set; } = new();
    public List<GrabarVentaFormaPagoDto> FormasPago { get; set; } = new();
}

public class GrabarVentaDetalleDto
{
    public int     FkProducto     { get; set; }
    public decimal PrecioSinIva   { get; set; }
    public decimal PrecioConIva   { get; set; }
    public decimal Cantidad       { get; set; }
    public decimal Costo          { get; set; }
    public decimal Subtotal       { get; set; }
    public bool    Fraccionado    { get; set; }
    public decimal DescRec        { get; set; }   // negativo = descuento, positivo = recargo
    public decimal SubtotalSinIva { get; set; }
    public int     FkPedido       { get; set; }   // 0 = sin pedido
}

public class GrabarVentaFormaPagoDto
{
    public int     FkMedioPago { get; set; }
    public int     FkPlanPago  { get; set; }
    public decimal Importe     { get; set; }
    public string? Referencia1 { get; set; }
    public string? Referencia2 { get; set; }
    public string? Referencia3 { get; set; }
}

// ── Resultado guardar venta ──────────────────────────────────────────────────
public class GrabarVentaResultDto
{
    public bool   Ok       { get; set; }
    public long   VentaId  { get; set; }
    public string Msg      { get; set; } = "";
}

// ── Para impresión comprobante X ─────────────────────────────────────────────
public class VentaImpresionDto
{
    public long      VentaId          { get; set; }
    public DateTime? Fecha            { get; set; }
    public string?   NombreCliente    { get; set; }
    public string?   RazonSocial      { get; set; }
    public string?   Cuil             { get; set; }
    public string?   DireccionCliente { get; set; }
    public string?   Provincia        { get; set; }
    public string?   Email              { get; set; }
    public string?   CondIvaAbrev       { get; set; }
    public string?   CondIvaAbrevFE     { get; set; }
    public string?   CondIvaLetra       { get; set; }
    public string?   CondIvaDescripcion { get; set; }
    public decimal   Iva               { get; set; }
    public decimal?  Descuento        { get; set; }
    public decimal?  Recargo          { get; set; }
    public decimal   Impuesto         { get; set; }
    public decimal   TotalVenta       { get; set; }
    public int?      FkCliente        { get; set; }
    public string?   FormaPago        { get; set; }
    public List<VentaDetalleImpresionItemDto>  Detalle          { get; set; } = new();
    public List<FormaPagoImpresionDto>         FormasPagoDetalle { get; set; } = new();
}

public class FormaPagoImpresionDto
{
    public string  Nombre  { get; set; } = "";
    public string? Plan    { get; set; }
    public decimal Importe { get; set; }
}

public class VentaDetalleImpresionItemDto
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

// ── Precio actual de un producto ─────────────────────────────────────────────
public class ProductoPrecioActualDto
{
    public int     ProductoId   { get; set; }
    public decimal PrecioSinIva { get; set; }
    public decimal PrecioConIva { get; set; }
    public bool    Dolarizado   { get; set; }
}

// ── Resultado factura electrónica ────────────────────────────────────────────
public class FacturaElectronicaResultDto
{
    // Campos "legacy" (un solo comprobante). Si la venta se dividió en varias partes
    // (> 130 ítems), reflejan el ÚLTIMO comprobante emitido con éxito, para no romper
    // código que todavía no fue actualizado para leer Comprobantes. Ok = true solo si
    // se emitieron correctamente TODOS los comprobantes esperados.
    public bool          Ok              { get; set; }
    public string?       Cae             { get; set; }
    public string?       VencimientoCae  { get; set; }
    public string?       NumeroComprobante { get; set; }
    public string?       PdfUrl          { get; set; }
    public string?       QrAfip          { get; set; }
    public List<string>  Errores         { get; set; } = new();

    /// <summary>Un elemento por cada comprobante fiscal emitido para esta venta (normalmente 1).</summary>
    public List<ComprobanteEmitidoResultItemDto> Comprobantes { get; set; } = new();
}
