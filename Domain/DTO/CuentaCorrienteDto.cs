namespace Domain.DTO;

public class CuentaCorrienteDto
{
    public DateTime Fecha            { get; set; }
    public string   Movimiento       { get; set; } = "";
    public string?  NumeroReferencia { get; set; }
    public decimal  Debe             { get; set; }
    public decimal  Haber            { get; set; }
    public decimal  Saldo            { get; set; }
    public bool     EsCobro          { get; set; }
    public int?     CobroId          { get; set; }
}

public class EstadoCCDto
{
    /// <summary>Saldo acumulado de movimientos anteriores a los últimos 6 meses. Positivo = deuda.</summary>
    public decimal SaldoAnterior        { get; set; }
    public bool    TieneSaldoAnterior   { get; set; }
    /// <summary>Movimientos de los últimos 6 meses.</summary>
    public List<CuentaCorrienteDto> Movimientos { get; set; } = new();
    public decimal TotalDebe            { get; set; }
    public decimal TotalHaber           { get; set; }
    /// <summary>Saldo final = SaldoAnterior + TotalDebe - TotalHaber</summary>
    public decimal SaldoFinal           { get; set; }
}
