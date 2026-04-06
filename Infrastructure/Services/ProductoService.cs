using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
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
                CodProveedor = vm.CodProveedor,
                CodBarras = vm.CodBarras,
                Descripcion = vm.Descripcion,
                FkRubro = vm.FkRubro,
                FkProveedor = vm.FkProveedor,
                Iva = vm.FkIva ?? 1,
                Baja = false
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

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }


        public async Task ActualizarAsync(ProductoDetallesDTO producto)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var existente = await _context.Productos.FirstOrDefaultAsync(p => p.Id == producto.Id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró el producto Id={producto.Id}");

            existente.CodProveedor = producto.CodProveedor?.Trim();
            existente.CodBarras = producto.CodBarras?.Trim();
            existente.FkRubro = producto.FkRubro;            
            existente.Descripcion = producto.Descripcion?.Trim();
            existente.FkProveedor = producto.FkProveedor;

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
                Id = p.Id,
                CodBarras = p.CodBarras,
                CodProveedor = p.CodProveedor,
                Descripcion = p.Descripcion,
                Cantidad = s.Cantidad,
                CantidadMinima = s.CantidadMinima,
                Precio = pp.Precio,
                Costo = cp.Costo,
                Rubro = r.Descripcion,
                Proveedor = prov.NombreComercial,
                PrecioProveedor = prpr.Precio,
                FkRubro = p.FkRubro,
                FkProveedor = p.FkProveedor
            }).FirstOrDefaultAsync();


            if (producto != null)
            {
                producto.Cantidad = Math.Round(producto.Cantidad ?? 0, decStock);
                producto.CantidadMinima = Math.Round(producto.CantidadMinima ?? 0, decStock);
                producto.Precio = Math.Round(producto.Precio ?? 0, decCant);
                producto.Costo = Math.Round(producto.Costo ?? 0, decCant);
                producto.PrecioProveedor = Math.Round(producto.PrecioProveedor ?? 0, decCant);
            }

            return producto;

        }
       
    }
}