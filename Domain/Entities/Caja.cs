namespace Domain.Entities;

public class Caja
{
    public int       CajaId        { get; set; }
    public int       UsuarioId     { get; set; }
    public DateTime  FechaApertura { get; set; }
    public DateTime? FechaCierre   { get; set; }
    public decimal   SaldoInicial  { get; set; }
    public decimal?  SaldoCierre   { get; set; }
    /// <summary>'ABIERTA' o 'CERRADA' (enum en BD)</summary>
    public string    Estado        { get; set; } = null!;
    public string?   Observaciones { get; set; }
}
