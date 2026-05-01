namespace Domain.DTO;

// ── Venta sin facturar (equivalente a sp_Ventas_TraerSinFacturarPorFecha) ──
public class VentaNoFacturadaDto
{
    public long     Nro             { get; set; }
    public DateTime Fecha           { get; set; }
    public decimal  TotalVenta      { get; set; }
    public string   NombreComercial { get; set; } = "";
    public string   Cajero          { get; set; } = "";
    public decimal  Iva             { get; set; }
    public decimal  Descuento       { get; set; }
    public decimal  Recargo         { get; set; }
    public decimal  Impuesto        { get; set; }
}
