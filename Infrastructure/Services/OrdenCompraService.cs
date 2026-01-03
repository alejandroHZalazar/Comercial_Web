using Application.Interfaces;
using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Application.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly ComercialDbContext _context;

        public OrdenCompraService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrdenCompra>> GetOrdenesAsync()
        {
            return await _context.OrdenCompras
                .Where(o => o.Procesado != true)
                .OrderByDescending(o => o.Fecha)
                .ToListAsync();
        }

        public async Task<OrdenCompra?> GetOrdenByIdAsync(long id)
        {
            return await _context.OrdenCompras
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<OrdenCompraDetalle>> GetDetallesByOrdenIdAsync(long ordenId)
        {
            return await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == ordenId)
                .OrderBy(d => d.Linea)
                .ToListAsync();
        }

        public async Task<long> CrearOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles)
        {
            orden.Fecha ??= DateTime.Today;
            orden.Procesado = false;
            orden.Total = detalles.Sum(d => d.Subtotal ?? 0m);

            _context.OrdenCompras.Add(orden);
            await _context.SaveChangesAsync();

            long nuevoId = orden.Id;

            long linea = 1;
            foreach (var d in detalles)
            {
                d.FkOrdenCompra = nuevoId;
                d.Linea = linea++;
                d.Subtotal = (d.Cantidad ?? 0m) * (d.PrecioProveedor ?? 0m);
                d.Procesado = false;
                _context.OrdenCompraDetalles.Add(d);
            }

            await _context.SaveChangesAsync();
            return nuevoId;
        }

        public async Task ActualizarOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles)
        {
            var existente = await _context.OrdenCompras.FirstOrDefaultAsync(o => o.Id == orden.Id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró la orden Id={orden.Id}");

            existente.Fecha = orden.Fecha;
            existente.FkProveedor = orden.FkProveedor;
            existente.Iva = orden.Iva;
            existente.Recargo = orden.Recargo;
            existente.Descuento = orden.Descuento;
            existente.Total = detalles.Sum(d => d.Subtotal ?? 0m);

            var existentesDetalles = await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == orden.Id)
                .ToListAsync();

            _context.OrdenCompraDetalles.RemoveRange(existentesDetalles);

            long linea = 1;
            foreach (var d in detalles)
            {
                d.FkOrdenCompra = orden.Id;
                d.Linea = linea++;
                d.Subtotal = (d.Cantidad ?? 0m) * (d.PrecioProveedor ?? 0m);
                d.Procesado = false;
                _context.OrdenCompraDetalles.Add(d);
            }

            await _context.SaveChangesAsync();
        }

        public async Task EliminarOrdenAsync(long id)
        {
            var orden = await _context.OrdenCompras.FirstOrDefaultAsync(o => o.Id == id);
            if (orden == null) return;

            var detalles = await _context.OrdenCompraDetalles
                .Where(d => d.FkOrdenCompra == id)
                .ToListAsync();

            _context.OrdenCompraDetalles.RemoveRange(detalles);
            _context.OrdenCompras.Remove(orden);

            await _context.SaveChangesAsync();
        }
    }
}