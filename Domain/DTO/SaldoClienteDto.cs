namespace Domain.DTO;

public class SaldoClienteDto
{
    public int      ClienteId            { get; set; }
    public string?  NombreComercial      { get; set; }
    public string?  RazonSocial          { get; set; }
    public string?  Cuil                 { get; set; }
    public string?  Direccion            { get; set; }
    public string?  LocalidadDescripcion { get; set; }
    public string?  ProvinciaDescripcion { get; set; }
    public string?  ZonaDescripcion      { get; set; }
    public string?  Vendedor             { get; set; }
    public string?  Telefono             { get; set; }
    public string?  Email                { get; set; }
    public decimal  TotalDebe            { get; set; }
    public decimal  TotalHaber           { get; set; }
    public decimal  Saldo                { get; set; }
}
