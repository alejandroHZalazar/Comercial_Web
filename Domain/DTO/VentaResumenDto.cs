using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class VentaResumenDto
    {
        public long Nro { get; set; }
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public decimal IVA { get; set; }
        public decimal Desc_Rec { get; set; }
        public decimal TotalCIVA { get; set; }
        public decimal totalSIVA { get; set; }
        public decimal Costo { get; set; }
        public decimal P_Com { get; set; }
        public decimal Comision { get; set; }
    }

}
