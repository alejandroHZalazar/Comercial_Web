namespace Domain.DTO;

/// <summary>
/// Resultado de la emisión de UN comprobante fiscal individual. Una venta/devolución con más
/// de 130 ítems produce varios de estos (uno por "parte"); una operación dentro del límite
/// produce exactamente uno, con Parte = 1 y TotalPartes = 1.
/// </summary>
public class ComprobanteEmitidoResultItemDto
{
    public bool     Ok                { get; set; }
    public int      Parte             { get; set; }
    public int      TotalPartes       { get; set; }
    public int      CantidadItems     { get; set; }
    public decimal  Importe           { get; set; }
    public string?  Cae               { get; set; }
    public string?  VencimientoCae    { get; set; }
    public string?  NumeroComprobante { get; set; }
    public string?  PdfUrl            { get; set; }
    public string?  QrAfip            { get; set; }
    public string?  Error             { get; set; }
}

/// <summary>Resultado de emitir una Nota de Crédito electrónica (puede ser más de un comprobante).</summary>
public class NotaCreditoElectronicaResultDto
{
    /// <summary>true solo si se emitieron correctamente TODOS los comprobantes esperados.</summary>
    public bool    Ok      { get; set; }
    public string? Error   { get; set; }
    /// <summary>Datos del último comprobante emitido con éxito, para compatibilidad con código que no lee Comprobantes.</summary>
    public string? PdfUrl  { get; set; }
    public List<ComprobanteEmitidoResultItemDto> Comprobantes { get; set; } = new();
}
