namespace Domain.Entities;

public class VentaPromoComponente
{
    public int     Id             { get; set; }
    public long    FkVentaDetalle { get; set; }
    public int     FkSlot         { get; set; }
    public int     FkProducto     { get; set; }
    public decimal Cantidad       { get; set; }
}
