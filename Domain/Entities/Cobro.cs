namespace Domain.Entities;

public class Cobro
{
    public int      CobroId       { get; set; }
    public int      ClienteId     { get; set; }
    public DateTime Fecha         { get; set; }
    public decimal  ImporteTotal  { get; set; }
    public int?     EstadoId      { get; set; }
    public int      DocumentoId   { get; set; }   // NOT NULL en BD, inicial = 0
    public int?     TipoCobro     { get; set; }
    public string?  Observaciones { get; set; }
}
