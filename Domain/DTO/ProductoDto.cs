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
        // Porcentajes por producto (solo se persisten si el proveedor tiene preciosPorProducto = 1).
        public decimal? Ganancia { get; set; }
        public decimal? Descuento { get; set; }
        public decimal? CantidadMinimaVenta { get; set; }
        public string? DescripcionLarga { get; set; }
        /// <summary>
        /// Legacy: columna Productos.imagen (una sola imagen). Se mantiene sin cambios por
        /// transición segura; el ABM ya no la usa en la UI, reemplazada por Imagenes (imagenesProductos).
        /// En POST: base64 de la imagen nueva (vacío = no cambiar en edición).
        /// </summary>
        public string? Imagen { get; set; }
        /// <summary>Legacy: true si el producto tiene imagen guardada en Productos.imagen. Ver Imagenes.</summary>
        public bool TieneImagen { get; set; }

        /// <summary>Metadatos (sin blob) de las imágenes activas del producto, ordenadas por Orden.</summary>
        public List<ImagenProductoDto> Imagenes { get; set; } = new();

        /// <summary>
        /// Solo en ALTA (Id == 0): JSON con las imágenes cargadas antes de guardar el producto
        /// (aún no existe FK). Se persisten en la misma transacción que crea el producto.
        /// Lista de <see cref="ImagenProductoNuevaDto"/> serializada.
        /// </summary>
        public string? ImagenesNuevasJson { get; set; }
    }

    /// <summary>Metadatos (sin blob) de una imagen ya persistida en imagenesProductos.</summary>
    public class ImagenProductoDto
    {
        public int Id { get; set; }
        public bool EsPrincipal { get; set; }
        public int Orden { get; set; }
    }

    /// <summary>Imagen pendiente de persistir, cargada durante el alta de un producto nuevo (aún sin Id).</summary>
    public class ImagenProductoNuevaDto
    {
        /// <summary>Base64 con o sin prefijo data:mime;base64,...</summary>
        public string Imagen { get; set; } = "";
        public bool EsPrincipal { get; set; }
        public int Orden { get; set; }
    }

}
