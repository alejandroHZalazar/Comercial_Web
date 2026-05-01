namespace Domain.DTO;

public class ImpresionEtiquetasItemDto
{
    public int     ProductoId      { get; set; }
    public string  CodProveedor    { get; set; } = string.Empty;
    public string  CodBarras       { get; set; } = string.Empty;
    public string  Descripcion     { get; set; } = string.Empty;
    public string  ProveedorNombre { get; set; } = string.Empty;
    public string  RubroNombre     { get; set; } = string.Empty;
    public decimal PrecioLista     { get; set; }
}
