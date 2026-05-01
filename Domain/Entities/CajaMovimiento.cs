namespace Domain.Entities;

/// <summary>
/// Movimiento dentro de una caja (tabla: movimientos_caja).
/// Equivale a sp_caja_AddMovimientoSinTransaccion.
/// </summary>
public class CajaMovimiento
{
    public int      MovimientoCajaId { get; set; }
    public int      CajaId           { get; set; }
    public DateTime Fecha            { get; set; }
    public int      ConceptoCajaId   { get; set; }   // NOT NULL en BD
    public int      MedioPagoId      { get; set; }   // NOT NULL en BD
    public decimal  Importe          { get; set; }
    public string?  Observaciones    { get; set; }
}
