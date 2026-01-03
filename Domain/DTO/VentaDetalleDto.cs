using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class VentaDetalleDto
    {
        public long VentaId { get; set; }
        public string CodProv { get; set; } = string.Empty;
        public string CodBarras { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal PrecioSIVA { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }

}
