namespace Domain.DTO
{
    public class RankingClienteDetalleDto
    {
        public string Cliente { get; set; } = "";
        public decimal TotalSinIva { get; set; }
        public decimal TicketPromedio { get; set; }
        public int CantCompras { get; set; }
        public int ProdDistintos { get; set; }
        public decimal Participacion { get; set; }
        public decimal Costo { get; set; }
        public decimal Rentabilidad { get; set; }
        public decimal Margen { get; set; }
    }
}
