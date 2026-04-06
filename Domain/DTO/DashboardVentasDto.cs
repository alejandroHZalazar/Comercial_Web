namespace Domain.DTO;

public class DashboardVentasDto
{
    public List<DashboardSerieDto> VentasPorDia       { get; set; } = new();
    public List<DashboardSerieDto> VentasPorCliente   { get; set; } = new();
    public List<DashboardSerieDto> MediosPago         { get; set; } = new();
    public List<DashboardSerieDto> VentasPorProveedor { get; set; } = new();
    public List<DashboardSerieDto> VentasPorRubro     { get; set; } = new();
}
