using Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ViewModel
{
    public class VentaDetalleViewModel
    {
        public long VentaId { get; set; }
        public List<VentaDetalleDto> Items { get; set; } = new();
    }

}
