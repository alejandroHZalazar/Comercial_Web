namespace Domain.DTO;

// ─── Input: una línea de forma de pago enviada desde el frontend ───────────
public class CobroItemDto
{
    public int     MedioPagoId { get; set; }
    public string? MedioNombre { get; set; }
    public decimal Importe     { get; set; }
    public string? Referencia1 { get; set; }
    public string? Referencia2 { get; set; }
    public string? Referencia3 { get; set; }
}

// ─── Input: request completa del cobro ────────────────────────────────────
public class CobroRequestDto
{
    public int                ClienteId    { get; set; }
    public decimal            ImporteTotal { get; set; }
    public List<CobroItemDto> Detalle      { get; set; } = new();
}

// ─── Output: datos del cobro para el recibo ───────────────────────────────
public class CobroReciboDto
{
    public int      CobroId       { get; set; }
    public DateTime Fecha         { get; set; }
    public decimal  ImporteTotal  { get; set; }

    // Datos del cliente
    public string? NombreComercial       { get; set; }
    public string? RazonSocial           { get; set; }
    public string? Direccion             { get; set; }
    public string? LocalidadDescripcion  { get; set; }
    public string? ProvinciaDescripcion  { get; set; }
    public string? Telefono              { get; set; }
    public string? Cuil                  { get; set; }

    // Saldo de CC posterior al cobro
    public decimal SaldoPostCobro  { get; set; }

    public List<CobroDetalleReciboItem> Detalle { get; set; } = new();
}

public class CobroDetalleReciboItem
{
    public string? MedioPago  { get; set; }
    public decimal Importe    { get; set; }
    public string? Referencia1 { get; set; }
    public string? Referencia2 { get; set; }
    public string? Referencia3 { get; set; }
}

// ─── Output: medio de pago con sus planes ─────────────────────────────────
public class PlanPagoConMedioDto
{
    public int     PlanId     { get; set; }
    public string  PlanNombre { get; set; } = "";
    public int     MedioId    { get; set; }
    public string  MedioNombre { get; set; } = "";
    public bool    ConDatos   { get; set; }
    public decimal Recargo    { get; set; }
}
