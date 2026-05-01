using System.Text.Json.Serialization;

namespace Domain.DTO;

public class NotaCreditoRequestDto
{
    public int      ClienteId           { get; set; }
    public decimal  Importe             { get; set; }
    public int?     FacturaAsociada     { get; set; }
    public string?  FechaFacturaAsoc    { get; set; }   // "dd/MM/yyyy"
    public decimal  IvaPorcentaje       { get; set; }

    [JsonPropertyName("iIBBPorcentaje")]
    public decimal  IIBBPorcentaje      { get; set; }

    public string   Observaciones       { get; set; } = "";
}

public class NotaCreditoDatosIniciales
{
    public bool                    FacturaElectronica { get; set; }
    public List<IvaItemDto>        Ivas               { get; set; } = new();
    public List<ImpuestoItemDto>   Impuestos          { get; set; } = new();
}

public class IvaItemDto   { public int Id { get; set; } public decimal Valor { get; set; } }
public class ImpuestoItemDto { public int Id { get; set; } public decimal Valor { get; set; } }

public class NotaDebitoRequestDto
{
    public int     ClienteId    { get; set; }
    public decimal Importe      { get; set; }
    public string  Observaciones { get; set; } = "";
}
