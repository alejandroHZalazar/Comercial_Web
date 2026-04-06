namespace Domain.DTO;

public class ProveedorGraficosDto
{
    /// <summary>Etiquetas del eje X (fechas) para el gráfico de curvas.</summary>
    public List<string>        CurvasLabels   { get; set; } = new();

    /// <summary>Una serie por proveedor, con un valor por cada fecha en CurvasLabels.</summary>
    public List<CurvaSerieDto> CurvasSeries   { get; set; } = new();

    /// <summary>Torta: compras agrupadas por proveedor.</summary>
    public List<DashboardSerieDto> TortaProveedor { get; set; } = new();

    /// <summary>Torta: compras agrupadas por rubro.</summary>
    public List<DashboardSerieDto> TortaRubro     { get; set; } = new();
}
