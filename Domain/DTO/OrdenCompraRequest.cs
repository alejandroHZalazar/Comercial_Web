using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class OrdenCompraRequest
    {
        public int ProveedorId { get; set; }
        public decimal Total { get; set; }
        public decimal Iva { get; set; }
        public decimal Recargo { get; set; }
        public decimal Descuento { get; set; }
        public List<OrdenCompraDetalle> Detalles { get; set; } = new();
    }

    public class OrdenCompraPrint
    {
        public string? id { get; set; }
        public DateTime? fecha { get; set; }
        public int? fk_proveedor { get; set; }
        public decimal? total { get; set; }
        public bool? procesado { get; set; }
        public decimal? iva { get; set; }
        public decimal? recargo { get; set; }
        public decimal? descuento { get; set; }

        public int? fk_producto { get; set; }
        public string? codBarras { get; set; }
        public string? codProveedor { get; set; }
        public string? descripcion { get; set; }
        public decimal? precioProveedor { get; set; }
        public decimal? cantidad { get; set; }
        public decimal? subtotal { get; set; }

        public byte[] imagen { get; set; }

        public string? nombreComercial { get; set; }
        public string? direccion { get; set; }

    }
}
