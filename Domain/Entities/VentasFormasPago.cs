namespace Domain.Entities;

public class VentasFormasPago
{
    public int Id { get; set; }
    public int FkVenta { get; set; }
    public int FkMedioPago { get; set; }
    public int FkPlanPago { get; set; }
    public decimal Importe { get; set; }
    public string? Referencia1 { get; set; }
    public string? Referencia2 { get; set; }
    public string? Referencia3 { get; set; }
}
