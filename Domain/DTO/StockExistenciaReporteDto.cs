namespace Domain.DTO;

/// <summary>Datos necesarios para generar el PDF "Listado de Stock en Existencia".</summary>
public class StockExistenciaReporteDto
{
    public string   EmpresaNombre    { get; set; } = string.Empty;
    public string   EmpresaRazon     { get; set; } = string.Empty;
    public string   EmpresaDireccion { get; set; } = string.Empty;
    public string   EmpresaContacto  { get; set; } = string.Empty;

    /// <summary>Logo de la empresa (JPEG o PNG); null si no hay.</summary>
    public byte[]?  Logo             { get; set; }

    public DateTime FechaGeneracion  { get; set; }

    /// <summary>Decimales con que se muestra el stock (parámetro productos/decimalesCant).</summary>
    public int      DecimalesStock   { get; set; } = 2;

    public List<StockExistenciaDto> Filas { get; set; } = new();
}
