using Application.Interfaces;
using Domain.Contracts;
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

        public async Task<Producto?> TraerPorIdAsync(int id)
        {
            return await _context.Productos
                .FirstOrDefaultAsync(p => p.Id == id && p.Baja != true);
        }

        public async Task<List<Producto>> BuscarPorCodProveedorAsync(string codProveedor)
        {
            return await _context.Productos
                .Where(p => p.CodProveedor != null &&
                            p.CodProveedor.Contains(codProveedor) &&
                            p.Baja != true)
                .OrderBy(p => p.Descripcion)
                .ToListAsync();
        }

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
    }
}