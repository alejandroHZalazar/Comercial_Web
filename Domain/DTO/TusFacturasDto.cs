namespace Domain.DTO;

public class TfFacturaRequest
{
    public string? usertoken  { get; set; }
    public string? apikey     { get; set; }
    public string? apitoken   { get; set; }
    public TfCliente?      cliente     { get; set; }
    public TfComprobante?  comprobante { get; set; }
}

public class TfCliente
{
    public string? documento_tipo       { get; set; }
    public string? documento_nro        { get; set; }
    public string? razon_social         { get; set; }
    public string? email                { get; set; }
    public string? domicilio            { get; set; }
    public string? provincia            { get; set; }
    public string? codigo               { get; set; }
    public string? envia_por_mail       { get; set; }
    public string? condicion_pago       { get; set; }
    public string? condicion_pago_otra  { get; set; }
    public string? condicion_iva        { get; set; }
    public string? rg5329               { get; set; }
}

public class TfComprobante
{
    public string? fecha                          { get; set; }
    public string? tipo                           { get; set; }
    public string? punto_venta                    { get; set; }
    public string? operacion                      { get; set; }
    public string? idioma                         { get; set; }
    public string? moneda                         { get; set; }
    public string? cotizacion                     { get; set; }
    public string? rubro                          { get; set; }
    public string? rubro_grupo_contable           { get; set; }
    public string? vencimiento                    { get; set; }
    public string? periodo_facturado_desde        { get; set; }
    public string? periodo_facturado_hasta        { get; set; }
    public decimal total                          { get; set; }
    // Bonificación general (importe SIN IVA) sobre el subtotal. Solo se serializa cuando es != 0
    // para no alterar el payload de los comprobantes sin descuento general.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public decimal bonificacion                   { get; set; }
    public List<TfDetalle>?            detalle                  { get; set; }
    public List<TfTributo>?            tributos                 { get; set; }
    public List<TfComprobanteAsociado>? comprobantes_asociados  { get; set; }
}

public class TfDetalle
{
    public decimal  cantidad                 { get; set; }
    public string?  afecta_stock             { get; set; }
    public decimal  bonificacion_porcentaje  { get; set; }
    public TfProducto? producto             { get; set; }
}

public class TfProducto
{
    public string?  descripcion              { get; set; }
    public int      unidad_bulto             { get; set; }
    public string?  lista_precios            { get; set; }
    public decimal  precio_unitario_sin_iva  { get; set; }
    public string?  codigo                   { get; set; }
    public decimal  alicuota                 { get; set; }
    public int      unidad_medida            { get; set; }
    public string?  actualiza_precio         { get; set; }
    public string?  rg5329                   { get; set; }
}

public class TfTributo
{
    public int     tipo            { get; set; }
    public int     regimen         { get; set; }
    public decimal base_imponible  { get; set; }
    public decimal alicuota        { get; set; }
    public decimal total           { get; set; }
}

public class TfComprobanteAsociado
{
    public string? tipo_comprobante  { get; set; }
    public int     punto_venta       { get; set; }
    public int     numero            { get; set; }
    public string? comprobante_fecha { get; set; }
    public long    cuit              { get; set; }
}

public class TfFacturaResponse
{
    public string?        error               { get; set; }
    public List<string>?  errores             { get; set; }
    public string?        cae                 { get; set; }
    public string?        vencimiento_cae     { get; set; }
    public string?        comprobante_pdf_url { get; set; }
    public string?        afip_qr             { get; set; }
    public string?        comprobante_nro     { get; set; }
}
