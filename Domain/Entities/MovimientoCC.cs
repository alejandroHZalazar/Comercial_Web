namespace Domain.Entities;

public class MovimientoCC
{
    public int      MovimientoId    { get; set; }
    public int      ClienteId       { get; set; }
    public int?     DocumentoId     { get; set; }
    public DateTime Fecha           { get; set; }
    /// <summary>'D' = Débito (deuda), 'C' = Crédito (pago)</summary>
    public string   TipoMovimiento  { get; set; } = null!;
    public decimal  Importe         { get; set; }
    public decimal  SaldoPendiente  { get; set; }
}
