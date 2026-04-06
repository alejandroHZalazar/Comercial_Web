namespace Domain.DTO;

/// <summary>
/// Una serie del gráfico de curvas multi-proveedor.
/// </summary>
public class CurvaSerieDto
{
    public string        Label { get; set; } = "";
    public List<decimal> Data  { get; set; } = new();
}
