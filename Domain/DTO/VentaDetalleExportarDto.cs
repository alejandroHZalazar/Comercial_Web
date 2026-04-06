namespace Domain.DTO;

public class VentaDetalleExportarDto
{
    public long Venta { get; set; }
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string CodProveedor { get; set; } = "";
    public string Producto { get; set; } = "";
    public decimal Precio { get; set; }
    public decimal Descuento { get; set; }
    public decimal Recargo { get; set; }
    public decimal PrecioSinIva { get; set; }
    public decimal Cantidad { get; set; }
    public decimal Costo { get; set; }
    public decimal Subtotal { get; set; }
}
