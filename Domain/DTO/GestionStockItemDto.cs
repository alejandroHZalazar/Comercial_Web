namespace Domain.DTO;

public class GestionStockItemDto
{
    public int     ProductoId      { get; set; }
    public string  CodProveedor    { get; set; } = string.Empty;
    public string  CodBarras       { get; set; } = string.Empty;
    public string  Descripcion     { get; set; } = string.Empty;
    public decimal Stock           { get; set; }
    public decimal CantidadMinima  { get; set; }
    public decimal PrecioCosto     { get; set; }
    public decimal PrecioProveedor { get; set; }
    public decimal PrecioLista     { get; set; }
}

public class AjusteStockRequest
{
    public int     ProductoId { get; set; }
    public decimal NuevoStock { get; set; }
}

public class AjusteStockResultDto
{
    public bool   Success { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
