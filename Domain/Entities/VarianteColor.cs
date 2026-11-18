namespace Domain.Entities;

/// <summary>Maestro de colores para productos con variantes (tabla variantesColores).</summary>
public class VarianteColor
{
    public int     Id        { get; set; }
    public string  Nombre    { get; set; } = string.Empty;
    /// <summary>#RRGGBB opcional, solo para mostrar una muestra.</summary>
    public string? CodigoHex { get; set; }
    public int     Orden     { get; set; }
    public bool    Activa    { get; set; } = true;
}
