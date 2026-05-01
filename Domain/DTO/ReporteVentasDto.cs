namespace Domain.DTO;

public class ReporteVentaItem
{
    public long      VentaId       { get; set; }
    public DateTime? Fecha         { get; set; }
    public int?      FkCliente     { get; set; }
    public string?   NombreCliente { get; set; }
    public string?   Factura       { get; set; }
    public decimal   TotalGeneral  { get; set; }
    public decimal   TotalCosto    { get; set; }
    public decimal   IvaPct        { get; set; }
    public decimal   IibbPct       { get; set; }
    public List<ReporteVentaFormaPago> FormasPago { get; set; } = new();
}

public class ReporteVentaFormaPago
{
    public string?  MedioPago { get; set; }
    public decimal  Importe   { get; set; }
}
