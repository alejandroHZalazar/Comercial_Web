namespace Domain.Entities;

public class Imputacion
{
    public int      ImputacionId        { get; set; }
    public int      MovimientoDebitoId  { get; set; }
    public int      MovimientoCreditoId { get; set; }
    public decimal  Importe             { get; set; }
    public DateTime Fecha               { get; set; }
}
