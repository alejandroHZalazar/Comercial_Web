using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public string? CodProveedor { get; set; }
        public string? CodBarras { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Cantidad { get; set; }
        public decimal? CantidadMinima { get; set; }
        public decimal? Precio { get; set; }
    }

   public class ProductoDetallesDTO
    {
        public int? Id { get; set; }
        public string? CodBarras { get; set; }
        public string? CodProveedor { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Cantidad { get; set; }
        public decimal? CantidadMinima { get; set; }
        public decimal? Precio { get; set; }
        public decimal? Costo { get; set; }
        public int? FkIva { get; set; }
        public decimal? Iva { get; set; }
        public string? Rubro { get; set; }
        public string? Proveedor { get; set; }
        public decimal? PrecioProveedor { get; set; }
        public int? FkRubro { get; set; }
        public int? FkProveedor { get; set; }
    }


}
