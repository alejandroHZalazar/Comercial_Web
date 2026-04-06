namespace Domain.DTO;

public class RankingProductoDetalleDto
{
    public string Proveedor { get; set; } = "";
    public string CodProv { get; set; } = "";
    public string Producto { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal TotalSinIva { get; set; }
    public decimal PrecioPromedio { get; set; }
    public decimal Participacion { get; set; }
    public decimal Costo { get; set; }
    public decimal Rentabilidad { get; set; }
    public int CantidadVentas { get; set; }
}
