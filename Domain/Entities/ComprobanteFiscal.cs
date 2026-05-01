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
    public int?      NumeroJornada        { get; set; }
    public DateTime  CreatedAt            { get; set; }
    public string?   AfipQr               { get; set; }
    public string?   LinkPdf              { get; set; }
}
