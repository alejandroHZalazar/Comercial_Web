namespace Domain.Entities;

public class PromocionSlotProducto
{
    public int Id         { get; set; }
    public int FkSlot     { get; set; }
    public int FkProducto { get; set; }
}
