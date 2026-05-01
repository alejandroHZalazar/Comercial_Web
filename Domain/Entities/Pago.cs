namespace Domain.Entities;

public class Pago
{
    public int      PagoId        { get; set; }
    public int      ProveedorId   { get; set; }
    public DateTime Fecha         { get; set; }
    public decimal  ImporteTotal  { get; set; }
    public string?  Observaciones { get; set; }
}
