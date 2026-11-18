namespace Domain.Entities;

/// <summary>Maestro de medidas para productos con variantes (tabla variantesMedidas).</summary>
public class VarianteMedida
{
    public int    Id     { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int    Orden  { get; set; }
    public bool   Activa { get; set; } = true;
}
