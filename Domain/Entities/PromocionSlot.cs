namespace Domain.Entities;

public class PromocionSlot
{
    public int      Id                { get; set; }
    public int      FkPromocion       { get; set; }
    public int      Numero            { get; set; }
    public decimal  CantidadRequerida { get; set; }
    public string?  Descripcion       { get; set; }
}
