namespace Domain.Entities;

public class Promocion
{
    public int       Id         { get; set; }
    public int       FkProducto { get; set; }
    public bool      Activa     { get; set; } = true;
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
