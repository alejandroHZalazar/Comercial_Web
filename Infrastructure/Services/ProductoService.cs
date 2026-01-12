using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

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
        public async Task<ProductoLineaOCDto?> TraerPorIdOCAsync(int id)
        {
           var query = from p in _context.Productos
                        join pr in _context.PreciosProveedores
                            on p.Id equals pr.FkProducto into precios
                        join s in _context.StockProductos on p.Id equals s.FkProducto
                        from pr in precios.DefaultIfEmpty()
                        where p.Id == id
                              && p.Baja != true                              
                        select new ProductoLineaOCDto
                        {
                            Id = p.Id,
                            CodBarras = p.CodBarras,
                            CodProveedor = p.CodProveedor,
                            Descripcion = p.Descripcion,
                            Cantidad = s.Cantidad,
                            CantidadMinima = s.CantidadMinima ?? 0,
                            PrecioProveedor = pr.Precio ?? 0m
                        };

            return await query.FirstOrDefaultAsync();
        }
       
        public async Task<ProductoLineaOCDto?> BuscarPorCodProveedorOCAsync(string codProveedor, int proveedorId)
        {
            var codigo = codProveedor?.Trim();

            var query = from p in _context.Productos
                        join pr in _context.PreciosProveedores
                            on p.Id equals pr.FkProducto into precios
                        join s in _context.StockProductos on p.Id equals s.FkProducto
                        from pr in precios.DefaultIfEmpty()
                        where p.CodProveedor == codigo
                              && p.Baja != true
                              && p.FkProveedor == proveedorId
                        select new ProductoLineaOCDto
                        {
                            Id = p.Id,
                            CodBarras = p.CodBarras,
                            CodProveedor = p.CodProveedor,
                            Descripcion = p.Descripcion,
                            Cantidad = s.Cantidad,
                            CantidadMinima = s.CantidadMinima ?? 0,
                            PrecioProveedor = pr.Precio ?? 0m
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<ProductoLineaOCDto?> BuscarPorCodBarrasOCAsync(string codBarras, int proveedorId)
        {
            var query = from p in _context.Productos
                        join pr in _context.PreciosProveedores
                            on p.Id equals pr.FkProducto into precios
                        join s in _context.StockProductos on p.Id equals s.FkProducto
                        from pr in precios.DefaultIfEmpty()
                        where p.CodBarras == codBarras
                              && p.Baja != true
                              && p.FkProveedor == proveedorId
                        select new ProductoLineaOCDto
                        {
                            Id = p.Id,
                            CodBarras = p.CodBarras,
                            CodProveedor = p.CodProveedor,
                            Descripcion = p.Descripcion,
                            Cantidad = s.Cantidad,
                            CantidadMinima = s.CantidadMinima ?? 0,
                            PrecioProveedor = pr.Precio ?? 0m
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<ProductoLineaOCDto?> BuscarPorDescripcionExactaAsync(string descripcion, int proveedorId)
        {

            var query = from p in _context.Productos
                        join pr in _context.PreciosProveedores
                            on p.Id equals pr.FkProducto into precios
                        join s in _context.StockProductos on p.Id equals s.FkProducto
                        from pr in precios.DefaultIfEmpty()
                        where p.Descripcion == descripcion
                              && p.Baja != true
                              && p.FkProveedor == proveedorId
                        select new ProductoLineaOCDto
                        {
                            Id = p.Id,
                            CodBarras = p.CodBarras,
                            CodProveedor = p.CodProveedor,
                            Descripcion = p.Descripcion,
                            Cantidad = s.Cantidad,
                            CantidadMinima = s.CantidadMinima ?? 0,
                            PrecioProveedor = pr.Precio ?? 0m
                        };

            return await query.FirstOrDefaultAsync();
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

        public async Task<int> CrearAsync(Producto producto)
        {
            producto.Baja = false;
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return producto.Id;
        }

        public async Task ActualizarAsync(Producto producto)
        {
            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == producto.Id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró el producto Id={producto.Id}");

            existente.CodProveedor = producto.CodProveedor?.Trim();
            existente.CodBarras = producto.CodBarras?.Trim();
            existente.FkRubro = producto.FkRubro;
            existente.Iva = producto.Iva;
            existente.Descripcion = producto.Descripcion?.Trim();
            existente.FkProveedor = producto.FkProveedor;

            await _context.SaveChangesAsync();
        }

        public async Task EliminarAsync(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return;

            producto.Baja = true;
            await _context.SaveChangesAsync();
        }

        public Task<int> ObtenerDecimalesAsync()
        {
            // Simulación: podrías leer de tabla de parámetros
            return Task.FromResult(2);
        }

        public Task<int> ObtenerDecimalesStockAsync()
        {
            // Simulación: podrías leer de tabla de parámetros
            return Task.FromResult(0);
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

        public async Task<ProductoLineaOCDto?> TraerProductoParaEditarAsync(int productoId)
        {
            return await _context.Productos
                .Where(p => p.Id == productoId && p.Baja != true)
                .Select(p => new ProductoLineaOCDto
                {
                    Id = p.Id,
                    CodBarras = p.CodBarras,
                    CodProveedor = p.CodProveedor,
                    Descripcion = p.Descripcion,
                    Cantidad = 0, // stock actual si lo tenés
                    CantidadMinima = 0, // si lo tenés en otra tabla
                    PrecioProveedor = 0 // si lo tenés en otra tabla
                })
                .FirstOrDefaultAsync();
        }        

        public async Task<List<ProductoCantMinimaDto>> TraerCantMinPorProveedorAsync(int proveedorId)
        {
            return await (from p in _context.Productos
                          join s in _context.StockProductos on p.Id equals s.FkProducto
                          join pp in _context.PreciosProveedores on p.Id equals pp.FkProducto
                          where p.FkProveedor == proveedorId
                                && s.Cantidad <= s.CantidadMinima
                                && p.Baja != true
                          select new ProductoCantMinimaDto
                          {
                              CodBarras = p.CodBarras ?? "",
                              CodProveedor = p.CodProveedor ?? "",
                              Descripcion = p.Descripcion ?? "",
                              Cantidad = s.Cantidad ?? 0,
                              CantidadMinima = s.CantidadMinima ?? 0,
                              Precio = pp.Precio ?? 0,
                              Pedido = (s.CantidadMinima ?? 0) - (s.Cantidad ?? 0) + 1,
                              Id = p.Id
                          })
                          .ToListAsync();
        }

    }
}