namespace Domain.Entities
{
    public class CondicionIva
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
        public string Abrev { get; set; } = null!;
        public string Letra { get; set; } = null!;
        public string? AbrevFE { get; set; }
    }
}