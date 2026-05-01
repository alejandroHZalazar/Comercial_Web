namespace Domain.Entities;

public class NotaDebito
{
    public int      Id           { get; set; }
    public int      ClienteId    { get; set; }
    public DateTime Fecha        { get; set; }
    public decimal  ImporteTotal { get; set; }
    public int?     EstadoId     { get; set; }
    public string?  Observaciones { get; set; }
}
