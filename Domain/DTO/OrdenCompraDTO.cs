using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class OrdenCompraDTO
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
    public class ProductoLineaOCDto
    {
        public int Id { get; set; }
        public string? CodProveedor { get; set; }
        public string? CodBarras { get; set; }
        public string? Descripcion { get; set; }
        public decimal? Cantidad { get; set; }
        public decimal? CantidadMinima { get; set; }
        public decimal? PrecioProveedor { get; set; }
    }
    public class ProductoCantMinimaDto
    {
        public string CodBarras { get; set; } = "";
        public string CodProveedor { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal CantidadMinima { get; set; }
        public decimal Precio { get; set; }
        public decimal Pedido { get; set; }
        public int Id { get; set; }
    }

    public class ProductoAPedirOCDto
    {
        public string CodBarras { get; set; }
        public string CodProveedor { get; set; }
        public string Descripcion { get; set; }
        public decimal Stock { get; set; }
        public decimal CantidadMinima { get; set; }
        public decimal Ingreso { get; set; }
        public decimal Ventas { get; set; }
        public decimal PrecioProveedor { get; set; }
        public decimal PrecioLista { get; set; }
        public decimal Costo { get; set; }
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; } = 0; // editable en front
    }

    public class ProductoAPedirImprimirOCDto
    {
        public string CodBarras { get; set; }
        public string Cod_Prov { get; set; }
        public string Descripcion { get; set; }
        public decimal Stock { get; set; }
        public decimal C_Min { get; set; }
        public decimal Ingreso { get; set; }
        public decimal Ventas { get; set; }
        public decimal P_Prov { get; set; }
        public decimal Costo { get; set; }
        public decimal P_Lista { get; set; }
        public int Prod { get; set; }
        public decimal Cant { get; set; } = 0; // editable en front
    }

    public class OrdenCompraBuscarDTO()
    {
        public long? Id { get; set; }
        public DateTime? Fecha { get; set; }
        public string? Proveedor { get; set; }
        public decimal? Recargo { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? Iva { get; set; }
        public decimal? Total { get; set; }        
    }

    public class OrdenCompraBuscarDetalleDTO()
    {
        public long? Linea { get; set; }

        public string? CodBarras { get; set; }
        public string? CodProveedor { get; set; }
        public string? Descripcion { get; set; }

        public decimal? Stock { get; set; }
        public decimal? CMin { get; set; }

        public decimal? Costo { get; set; }
        public decimal? PLista { get; set; }
        public decimal? PProveedorSinIva { get; set; }
        public decimal? PProveedorConIva { get; set; }

        public decimal? Cantidad { get; set; }
        public decimal? Subtotal { get; set; }

        // Columnas ocultas / técnicas
        public long? IdOrdenCompra { get; set; }
        public int? Id { get; set; }
    }

}
