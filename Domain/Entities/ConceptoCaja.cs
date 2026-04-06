namespace Domain.Entities
{
    public class ConceptoCaja
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = null!;
        public string TipoMovimiento { get; set; } = null!; // "I" o "E"
        public bool AfectaEfectivo { get; set; }
        public int? FkMedioPago { get; set; }
        public string? Operacion { get; set; }
    }
}
