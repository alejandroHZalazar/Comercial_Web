namespace Domain.Entities
{
    public class DocumentoTipo
    {
        public int Id { get; set; }
        public string Abreviatura { get; set; } = null!;
        public string? Descripcion { get; set; }
    }
}
