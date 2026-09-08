namespace Domain.Entities;
public class ComprobanteFiscal
{
    public long      Id                   { get; set; }
    public string    TipoComprobante      { get; set; } = null!;
    public string    Letra                { get; set; } = null!;
    public int       PuntoVenta           { get; set; }
    public int       Numero               { get; set; }
    public DateTime  FechaEmision         { get; set; }
    public int?      NroReferencia        { get; set; }
    public int?      FkCliente            { get; set; }
    public string?   RazonSocial          { get; set; }
    public string?   Cuit                 { get; set; }
    public decimal   ImporteTotal         { get; set; }
    public string?   Cae                  { get; set; }
    public DateTime? FechaVencimientoCae  { get; set; }
    public string?   Estado               { get; set; }
    public int?      FiscalStatus         { get; set; }

    /// <summary>
    /// Número de parte (1-based) cuando una venta/devolución con más de 130 ítems se divide
    /// en varios comprobantes fiscales. null = comprobante no dividido (comportamiento normal).
    /// Reutiliza la columna física "numero_jornada" (sin uso previo en el sistema); no representa
    /// una jornada de caja pese al nombre de columna heredado.
    /// </summary>
    public int?      NroParte             { get; set; }
    public DateTime  CreatedAt            { get; set; }
    public string?   AfipQr               { get; set; }
    public string?   LinkPdf              { get; set; }
}
