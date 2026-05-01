namespace Domain.Entities;

public class Documento
{
    public int      ID            { get; set; }
    public int      ClienteId     { get; set; }
    /// <summary>FA = Venta, RE = Cobro, NC = Nota de Crédito, ND = Nota de Débito</summary>
    public string   TipoDocumento { get; set; } = null!;
    public string?  Numero        { get; set; }
    public DateTime Fecha         { get; set; }
    public decimal  Total         { get; set; }
}
