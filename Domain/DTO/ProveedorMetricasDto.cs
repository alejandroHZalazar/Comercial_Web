namespace Domain.DTO;

public class ProveedorMetricasDto
{
    public decimal TotalCompras    { get; set; }
    public int     CantidadCompras { get; set; }   // días distintos con compras
    public decimal PromedioCompras { get; set; }
    public string  MejorDia        { get; set; } = "-";
}
