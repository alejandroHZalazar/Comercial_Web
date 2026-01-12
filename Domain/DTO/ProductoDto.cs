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



}
