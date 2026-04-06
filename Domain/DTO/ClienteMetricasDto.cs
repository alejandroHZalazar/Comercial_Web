namespace Domain.DTO;

public class ClienteMetricasDto
{
    public decimal TotalVentas    { get; set; }
    public int     CantidadVentas { get; set; }
    public string  MejorDia       { get; set; } = "-";
    public decimal Promedio       { get; set; }
    public decimal Costos         { get; set; }
    public decimal Ganancias      { get; set; }
}
