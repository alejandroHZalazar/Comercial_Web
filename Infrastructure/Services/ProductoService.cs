using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using static Domain.DTO.ClienteDTO;

namespace Infrastructure.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ComercialDbContext _context;

        public ProductoService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> TraerTodosAsync()
        {
            return await _context.Productos
                .Where(p => p.Baja != true)
                .OrderBy(p => p.Descripcion)
                .ToListAsync();
        }

        #region Busquedas Ordenes de Compra

        public async Task<List<Producto>> GetByCodProveedorAsync(string codProveedor)
        {
            return await _context.Productos
                .Where(p => p.CodProveedor == codProveedor && p.Baja != true)
                .ToListAsync();
        }
        public async Task<List<Producto>> GetByCodProveedorProveedorAsync(string codProveedor, int proveedorId)
        {
            return await _context.Productos
                .Where(p => p.CodProveedor == codProveedor && p.FkProveedor == proveedorId && p.Baja != true)
                .ToListAsync();
        }

        public async Task<List<Producto>> GetByCodBarrasAsync(string codBarra)
        {
            return await _context.Productos
                .Where(p => p.CodBarras == codBarra && p.Baja != true)
                .ToListAsync();
        }

        public async Task<List<Producto>> GetByCodBarrasProveedorAsync(string codBarra, int proveedorId)
        {
            return await _context.Productos
                .Where(p => p.CodBarras == codBarra && p.FkProveedor == proveedorId && p.Baja != true)
                .ToListAsync();
        }

        #endregion
        public async Task<List<Producto>> BuscarPorDescripcionAsync(string descripcion)
        {
            return await _context.Productos
                .Where(p => p.Descripcion != null &&
                            p.Descripcion.Contains(descripcion) &&
                            p.Baja != true)
                .OrderBy(p => p.Descripcion)
                .ToListAsync();
        }

        public async Task CrearAsync(ProductoDetallesDTO vm)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var producto = new Producto
            {
                CodProveedor     = vm.CodProveedor,
                CodBarras        = vm.CodBarras,
                Descripcion      = vm.Descripcion,
                FkRubro          = vm.FkRubro,
                FkProveedor      = vm.FkProveedor,
                Iva              = vm.FkIva ?? 1,
                Baja             = false,
                Ganancia         = vm.Ganancia,
                Descuento        = vm.Descuento,
                DescripcionLarga = vm.DescripcionLarga?.Trim(),
                Imagen           = _Base64ABytes(vm.Imagen)
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            _context.PreciosProductos.Add(new PreciosProducto
            {
                FkProducto = producto.Id,
                Precio = vm.Precio
            });

            _context.CostosProductos.Add(new CostosProducto
            {
                FkProducto = producto.Id,
                Costo = vm.Costo
            });

            _context.PreciosProveedores.Add(new PreciosProveedore
            {
                FkProducto = producto.Id,
                Precio = vm.PrecioProveedor
            });

            _context.StockProductos.Add(new StockProducto
            {
                FkProducto = producto.Id,
                Cantidad = vm.Cantidad,
                CantidadMinima = vm.CantidadMinima
            });

            // Imágenes cargadas antes de guardar (aún no existía Id de producto).
            // Se insertan en la misma transacción para no dejar el alta a mitad de camino.
            _AgregarImagenesPendientes(producto.Id, vm.ImagenesNuevasJson);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }


        public async Task ActualizarAsync(ProductoDetallesDTO producto)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == producto.Id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró el producto Id={producto.Id}");

            existente.CodProveedor     = producto.CodProveedor?.Trim();
            existente.CodBarras        = producto.CodBarras?.Trim();
            existente.FkRubro          = producto.FkRubro;
            existente.Descripcion      = producto.Descripcion?.Trim();
            existente.FkProveedor      = producto.FkProveedor;
            existente.Ganancia         = producto.Ganancia;
            existente.Descuento        = producto.Descuento;
            existente.DescripcionLarga = producto.DescripcionLarga?.Trim();

            // Lógica de imagen:
            //   null/vacío  → mantener la imagen existente sin cambios
            //   "BORRAR"    → quitar imagen (null en BD)
            //   base64/data → reemplazar por los nuevos bytes
            if (producto.Imagen == "BORRAR")
                existente.Imagen = null;
            else
            {
                var nuevaImagen = _Base64ABytes(producto.Imagen);
                if (nuevaImagen != null)
                    existente.Imagen = nuevaImagen;
            }

            var precioProducto = await _context.PreciosProductos.FirstOrDefaultAsync(pp => pp.FkProducto == producto.Id);
            if (precioProducto == null)
                throw new InvalidOperationException($"No se encontró el Precio Producto Id={producto.Id}");
            precioProducto.Precio = producto.Precio;

            var costoProducto = await _context.CostosProductos.FirstOrDefaultAsync(cp => cp.FkProducto == producto.Id);
            if (costoProducto == null)
                throw new InvalidOperationException($"No se encontró el Costo Producto Id={producto.Id}");
            costoProducto.Costo = producto.Costo;

            var precioProveedor = await _context.PreciosProveedores.FirstOrDefaultAsync(cp => cp.FkProducto == producto.Id);
            if (precioProveedor == null)
                throw new InvalidOperationException($"No se encontró el Precio Proveedor Producto Id={producto.Id}");
            precioProveedor.Precio = producto.PrecioProveedor;

            var stock = await _context.StockProductos.FirstOrDefaultAsync(cp => cp.FkProducto == producto.Id);
            if (stock == null)
                throw new InvalidOperationException($"No se encontró el Stock Producto Id={producto.Id}");            
            stock.CantidadMinima = producto.CantidadMinima;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return;

            producto.Baja = true;
            await _context.SaveChangesAsync();
        }

        public async Task<List<ProductoDto>> TraerProductosProveedorAsync(int proveedorId)
        {
            return await _context.Productos
                .Where(p => p.FkProveedor == proveedorId && p.Baja != true)
                .Select(p => new ProductoDto
                {
                    Id = p.Id,
                    CodProveedor = p.CodProveedor,
                    CodBarras = p.CodBarras,
                    Descripcion = p.Descripcion
                })
                .OrderBy(p => p.Descripcion)
                .ToListAsync();
        }

        public async Task<byte[]?> ObtenerImagenAsync(int id)
        {
            // Solo trae la columna imagen, sin cargar el resto del producto
            return await _context.Productos
                .Where(p => p.Id == id)
                .Select(p => p.Imagen)
                .FirstOrDefaultAsync();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>
        /// Convierte base64 (con o sin prefijo data:...) a byte[].
        /// Devuelve null si el string está vacío o no es base64 válido.
        /// </summary>
        private static byte[]? _Base64ABytes(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            try
            {
                // Quitar prefijo "data:image/...;base64," si lo tiene
                var idx = base64.IndexOf(',');
                var datos = idx >= 0 ? base64[(idx + 1)..] : base64;
                return Convert.FromBase64String(datos);
            }
            catch { return null; }
        }

        public async Task<ProductoDetallesDTO> traerDetalleAsync(int id, int decCant, int decStock)
        {
            var producto = await (
            from p in _context.Productos
            join s in _context.StockProductos on p.Id equals s.FkProducto
            join pp in _context.PreciosProductos on p.Id equals pp.FkProducto
            join cp in _context.CostosProductos on p.Id equals cp.FkProducto
            join r in _context.Rubros on p.FkRubro equals r.Id
            join prov in _context.Proveedores on p.FkProveedor equals prov.Id
            join prpr in _context.PreciosProveedores on p.Id equals prpr.FkProducto
            where p.Id == id
            select new ProductoDetallesDTO
            {
                Id               = p.Id,
                CodBarras        = p.CodBarras,
                CodProveedor     = p.CodProveedor,
                Descripcion      = p.Descripcion,
                Cantidad         = s.Cantidad,
                CantidadMinima   = s.CantidadMinima,
                Precio           = pp.Precio,
                Costo            = cp.Costo,
                Rubro            = r.Descripcion,
                Proveedor        = prov.NombreComercial,
                PrecioProveedor  = prpr.Precio,
                FkRubro          = p.FkRubro,
                FkProveedor      = p.FkProveedor,
                Ganancia         = p.Ganancia,
                Descuento        = p.Descuento,
                DescripcionLarga = p.DescripcionLarga,
                // El blob no se incluye en el DTO general; se sirve vía OnGetImagenAsync.
                // Solo indicamos si tiene imagen para que la UI decida qué mostrar.
                TieneImagen      = p.Imagen != null && p.Imagen.Length > 0
            }).FirstOrDefaultAsync();


            if (producto != null)
            {
                producto.Cantidad = Math.Round(producto.Cantidad ?? 0, decStock);
                producto.CantidadMinima = Math.Round(producto.CantidadMinima ?? 0, decStock);
                producto.Precio = Math.Round(producto.Precio ?? 0, decCant);
                producto.Costo = Math.Round(producto.Costo ?? 0, decCant);
                producto.PrecioProveedor = Math.Round(producto.PrecioProveedor ?? 0, decCant);

                producto.Imagenes = await GetImagenesAsync(producto.Id!.Value);
            }

            return producto;

        }

        // ═══════════════════════════════════════════════════════════════════
        //  Múltiples imágenes (imagenesProductos)
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<ImagenProductoDto>> GetImagenesAsync(int idProducto)
        {
            return await _context.ImagenesProductos
                .Where(i => i.FkProducto == idProducto && !i.Baja)
                .OrderBy(i => i.Orden).ThenBy(i => i.Id)
                .Select(i => new ImagenProductoDto
                {
                    Id          = i.Id,
                    EsPrincipal = i.EsPrincipal,
                    Orden       = i.Orden
                })
                .ToListAsync();
        }

        public async Task<(byte[] Bytes, string? ContentType)?> ObtenerImagenItemAsync(int imagenId)
        {
            var img = await _context.ImagenesProductos
                .Where(i => i.Id == imagenId && !i.Baja)
                .Select(i => new { i.Imagen, i.ContentType })
                .FirstOrDefaultAsync();

            return img == null ? null : (img.Imagen, img.ContentType);
        }

        public async Task<ImagenProductoDto> AgregarImagenAsync(int idProducto, byte[] bytes, string? contentType, bool esPrincipal)
        {
            var activas = await _context.ImagenesProductos
                .Where(i => i.FkProducto == idProducto && !i.Baja)
                .ToListAsync();

            // Si es la primera imagen del producto, siempre queda como principal.
            bool nuevaEsPrincipal = esPrincipal || activas.Count == 0;

            if (nuevaEsPrincipal)
                foreach (var a in activas.Where(a => a.EsPrincipal))
                    a.EsPrincipal = false;

            int siguienteOrden = activas.Count == 0 ? 0 : activas.Max(a => a.Orden) + 1;

            var nueva = new ImagenesProducto
            {
                FkProducto  = idProducto,
                Imagen      = bytes,
                ContentType = contentType,
                EsPrincipal = nuevaEsPrincipal,
                Orden       = siguienteOrden,
                Baja        = false,
                FechaAlta   = DateTime.Now
            };
            _context.ImagenesProductos.Add(nueva);
            await _context.SaveChangesAsync();

            return new ImagenProductoDto { Id = nueva.Id, EsPrincipal = nueva.EsPrincipal, Orden = nueva.Orden };
        }

        public async Task EliminarImagenAsync(int imagenId)
        {
            var img = await _context.ImagenesProductos.FirstOrDefaultAsync(i => i.Id == imagenId && !i.Baja);
            if (img == null) return;

            img.Baja = true;

            // Si era la principal, promover a otra imagen activa restante (la de menor orden) para
            // no dejar el producto sin imagen principal mientras tenga imágenes activas.
            if (img.EsPrincipal)
            {
                var siguiente = await _context.ImagenesProductos
                    .Where(i => i.FkProducto == img.FkProducto && !i.Baja && i.Id != imagenId)
                    .OrderBy(i => i.Orden).ThenBy(i => i.Id)
                    .FirstOrDefaultAsync();
                if (siguiente != null)
                    siguiente.EsPrincipal = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarcarPrincipalAsync(int idProducto, int imagenId)
        {
            var activas = await _context.ImagenesProductos
                .Where(i => i.FkProducto == idProducto && !i.Baja)
                .ToListAsync();

            var objetivo = activas.FirstOrDefault(i => i.Id == imagenId);
            if (objetivo == null) return; // no pertenece al producto o está dada de baja

            foreach (var a in activas)
                a.EsPrincipal = a.Id == imagenId;

            await _context.SaveChangesAsync();
        }

        public async Task MoverImagenAsync(int idProducto, int imagenId, bool haciaArriba)
        {
            var activas = await _context.ImagenesProductos
                .Where(i => i.FkProducto == idProducto && !i.Baja)
                .OrderBy(i => i.Orden).ThenBy(i => i.Id)
                .ToListAsync();

            int idx = activas.FindIndex(i => i.Id == imagenId);
            int destino = haciaArriba ? idx - 1 : idx + 1;
            if (idx < 0 || destino < 0 || destino >= activas.Count) return; // ya está en el extremo

            // Intercambiar Orden entre la imagen y su vecina.
            (activas[idx].Orden, activas[destino].Orden) = (activas[destino].Orden, activas[idx].Orden);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Persiste (dentro de la transacción de alta) las imágenes cargadas antes de que el
        /// producto tuviera Id. Aplica la misma regla de "una sola principal" que AgregarImagenAsync.
        /// </summary>
        private void _AgregarImagenesPendientes(int idProducto, string? imagenesNuevasJson)
        {
            if (string.IsNullOrWhiteSpace(imagenesNuevasJson)) return;

            List<ImagenProductoNuevaDto>? pendientes;
            try
            {
                pendientes = JsonSerializer.Deserialize<List<ImagenProductoNuevaDto>>(
                    imagenesNuevasJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return; } // JSON inválido: no bloquea el alta del producto, simplemente no carga imágenes

            if (pendientes == null || pendientes.Count == 0) return;

            // No confiar en el cliente: garantizar una sola principal (la primera marcada, o la de orden 0).
            int idxPrincipal = pendientes.FindIndex(p => p.EsPrincipal);
            if (idxPrincipal < 0) idxPrincipal = 0;

            for (int i = 0; i < pendientes.Count; i++)
            {
                var bytes = _Base64ABytes(pendientes[i].Imagen);
                if (bytes == null) continue;

                _context.ImagenesProductos.Add(new ImagenesProducto
                {
                    FkProducto  = idProducto,
                    Imagen      = bytes,
                    ContentType = _DetectarContentTypeImagen(bytes),
                    EsPrincipal = i == idxPrincipal,
                    Orden       = i,
                    Baja        = false,
                    FechaAlta   = DateTime.Now
                });
            }
        }

        private static string _DetectarContentTypeImagen(byte[] data)
        {
            if (data.Length >= 2)
            {
                if (data[0] == 0xFF && data[1] == 0xD8) return "image/jpeg";
                if (data[0] == 0x89 && data[1] == 0x50) return "image/png";
                if (data[0] == 0x47 && data[1] == 0x49) return "image/gif";
                if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 &&
                    data[8] == 0x57 && data[9] == 0x45) return "image/webp";
            }
            return "image/jpeg";
        }

    }
}