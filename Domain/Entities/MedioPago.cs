namespace Domain.Entities
{
    public class MedioPago
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal? Recargo { get; set; }
        public bool? ConDatos { get; set; }
    }
}
