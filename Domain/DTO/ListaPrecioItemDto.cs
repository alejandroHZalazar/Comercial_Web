namespace Domain.DTO;

public class ListaPrecioItemDto
{
    public int     ProductoId   { get; set; }
    public string  CodProveedor { get; set; } = string.Empty;
    public string  CodBarras    { get; set; } = string.Empty;
    public string  Descripcion  { get; set; } = string.Empty;
    public decimal PrecioSinIva { get; set; }
    public decimal PrecioConIva { get; set; }
}
