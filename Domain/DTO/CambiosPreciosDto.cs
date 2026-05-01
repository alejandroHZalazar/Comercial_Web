namespace Domain.DTO;

public class CambiosPreciosItemDto
{
    public int     ProductoId   { get; set; }
    public string  CodProveedor { get; set; } = string.Empty;
    public string  CodBarras    { get; set; } = string.Empty;
    public string  Descripcion  { get; set; } = string.Empty;
    public decimal PProv        { get; set; }
    public decimal PSiva        { get; set; }
    public decimal Costo        { get; set; }
    public int     FkProveedor  { get; set; }
    public decimal Ganancia     { get; set; }
    public decimal Descuento    { get; set; }
}

/// <summary>
/// Request para actualizar los tres precios directamente (sin recalcular desde ganancia/descuento).
/// Usado cuando el usuario opta por ajuste independiente o edita manualmente.
/// </summary>
public class ActualizarPrecioDirectoRequest
{
    public string  CodProveedor { get; set; } = string.Empty;
    public string  CodBarras    { get; set; } = string.Empty;
    public int     IdProveedor  { get; set; }
    public decimal PProv        { get; set; }
    public decimal PSiva        { get; set; }
    public decimal Costo        { get; set; }
}
