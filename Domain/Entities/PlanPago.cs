namespace Domain.Entities
{
    public class PlanPago
    {
        public int Id { get; set; }
        public int? FkMedioPago { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal? Recargo { get; set; }
    }
}
