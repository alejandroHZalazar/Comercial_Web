namespace Domain.DTO
{
    public class AltaMasivaFilaDto
    {
        public string CodProveedor   { get; set; } = string.Empty;
        public string CodBarras      { get; set; } = string.Empty;
        public string Descripcion    { get; set; } = string.Empty;
        public int    FkRubro        { get; set; }
        public int    FkProveedor    { get; set; }
        public decimal PrecioProveedor { get; set; }
        public decimal Costo         { get; set; }
        public decimal PrecioLista   { get; set; }
        public decimal Stock         { get; set; }
        public decimal CantMinima    { get; set; }
        public bool   EsDolarizado   { get; set; }
    }

    public class AltaMasivaResultDto
    {
        public bool   Success    { get; set; }
        public string Mensaje    { get; set; } = string.Empty;
        public int    ProductoId { get; set; }
    }
}
