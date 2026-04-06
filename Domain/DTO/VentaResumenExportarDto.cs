namespace Domain.DTO;

public class VentaResumenExportarDto
{
    public long Nro { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal Costo { get; set; }
    public string Cliente { get; set; } = "";
    public string Cajero { get; set; } = "";
    public decimal Iva { get; set; }
    public decimal Descuento { get; set; }
    public decimal Recargo { get; set; }
    public string Vendedor { get; set; } = "";
    public decimal Comision { get; set; }
    public decimal Impuesto { get; set; }
    public string MedioPago1 { get; set; } = "Sin Especificar";
    public string MedioPago2 { get; set; } = "Sin Especificar";
    public string MedioPago3 { get; set; } = "Sin Especificar";
}
