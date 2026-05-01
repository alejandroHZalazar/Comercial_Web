namespace Domain.Entities;

public class CobroDetalle
{
    public int     CobroDetalleId { get; set; }   // PK real: CobroDetalleId
    public int     CobroId        { get; set; }
    public int     MedioPagoId    { get; set; }
    public decimal Importe        { get; set; }
    public string? Referencia1    { get; set; }
    public string? Referencia2    { get; set; }
    public string? Referencia3    { get; set; }
}
