namespace Domain.DTO;

public class ClienteGraficosDto
{
    /// <summary>Fechas (eje X) para el gráfico de curvas por cliente.</summary>
    public List<string>            CurvasLabels   { get; set; } = new();

    /// <summary>Una serie por cada cliente (top 10 por total).</summary>
    public List<CurvaSerieDto>     CurvasSeries   { get; set; } = new();

    /// <summary>Ventas agrupadas por Medio de Pago (barras).</summary>
    public List<DashboardSerieDto> MediosPago     { get; set; } = new();

    /// <summary>Ventas agrupadas por Proveedor (torta).</summary>
    public List<DashboardSerieDto> TortaProveedor { get; set; } = new();

    /// <summary>Ventas agrupadas por Rubro (torta).</summary>
    public List<DashboardSerieDto> TortaRubro     { get; set; } = new();

    /// <summary>Top 10 clientes deudores por SaldoPendiente (barras).</summary>
    public List<DashboardSerieDto> Deudores       { get; set; } = new();
}
