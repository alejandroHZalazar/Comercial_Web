using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class IngresoProductosDTO
    {
        public class ProductosPorProveedorDTO
        {
            public string? CodBarras { get; set; }
            public string? CodProv { get; set; }
            public string? Descripcion { get; set; }
            public decimal? Stock { get; set; }
            public decimal? Cmin { get; set; }
            public decimal? Costo { get; set; }
            public decimal? Precio { get; set; }
            public decimal? PrecProveedor { get; set; }
            public int Id { get; set; }
        }
        public class IngresoProductoDetalleDto
        {
            public int? ProductoId { get; set; }
            public decimal? Cantidad { get; set; }
            public decimal? Costo { get; set; }
            public decimal? Venta { get; set; }
            public decimal? PrecioProveedor { get; set; }
            public decimal? StockActual { get; set; }
            public string? NroComprobante { get; set; }
            public string? Descripcion { get; set; }
            public DateOnly FechaEntrega { get; set; }
        }
    }
}
