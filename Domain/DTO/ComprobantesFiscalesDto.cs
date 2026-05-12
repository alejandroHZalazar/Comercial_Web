namespace Domain.DTO;

// ── Lista principal ───────────────────────────────────────────────────────────
public class ComprobanteEmitidoDto
{
    public long      Id               { get; set; }
    public string    Letra            { get; set; } = "";
    public int       PuntoVenta       { get; set; }
    public int       Numero           { get; set; }
    public string    NumeroFormateado => $"{PuntoVenta:D4}-{Numero:D8}";
    public DateTime  FechaEmision     { get; set; }
    public long?     VentaId          { get; set; }
    public string?   RazonSocial      { get; set; }
    public string?   NombreComercial  { get; set; }
    public string?   Cuit             { get; set; }
    public decimal   ImporteTotal     { get; set; }
    public string?   Cae              { get; set; }
    public DateTime? FechaVtoCae      { get; set; }
    public string?   Estado           { get; set; }
    public string?   LinkPdf          { get; set; }
}

// ── Detalle de un comprobante ─────────────────────────────────────────────────
public class ComprobanteDetalleDto
{
    public long      ComprobanteId    { get; set; }
    public string    Letra            { get; set; } = "";
    public string    NumeroFormateado { get; set; } = "";
    public DateTime  FechaEmision     { get; set; }
    public string?   RazonSocial      { get; set; }
    public string?   NombreComercial  { get; set; }
    public string?   Cuit             { get; set; }
    public string?   Cae              { get; set; }
    public DateTime? FechaVtoCae      { get; set; }
    public string?   LinkPdf          { get; set; }

    // Datos de la venta asociada
    public long?     VentaId          { get; set; }
    public decimal   Iva              { get; set; }
    public decimal?  Descuento        { get; set; }
    public decimal?  Recargo          { get; set; }
    public decimal   Impuesto         { get; set; }
    public decimal   TotalVenta       { get; set; }

    public List<DetalleItemFEDto> Detalle    { get; set; } = new();
    public List<FormaPagoFEDto>   FormasPago { get; set; } = new();
}

public class DetalleItemFEDto
{
    public string?  Descripcion    { get; set; }
    public decimal  PrecioSinIva   { get; set; }
    public decimal  PrecioConIva   { get; set; }
    public decimal  Cantidad       { get; set; }
    public decimal  SubtotalSinIva { get; set; }
    public decimal? Descuento      { get; set; }
    public decimal? Recargo        { get; set; }
}

public class FormaPagoFEDto
{
    public string  MedioPago { get; set; } = "";
    public decimal Importe   { get; set; }
}

// ── Estadísticas ──────────────────────────────────────────────────────────────
public class EstadisticasFEDto
{
    public int     TotalEmitidas     { get; set; }
    public decimal TotalImporte      { get; set; }
    public decimal PromedioPorFact   { get; set; }

    public int     CantidadA         { get; set; }
    public decimal TotalA            { get; set; }
    public int     CantidadB         { get; set; }
    public decimal TotalB            { get; set; }
    public int     CantidadC         { get; set; }
    public decimal TotalC            { get; set; }
}

// ── Filtros de búsqueda ───────────────────────────────────────────────────────
public class BuscarComprobantesDto
{
    public DateTime  Desde         { get; set; }
    public DateTime  Hasta         { get; set; }
    public string?   FiltroCliente { get; set; }
    public string?   Letra         { get; set; }  // "A", "B", "C" o null = todos
}
