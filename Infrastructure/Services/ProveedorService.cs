using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly ComercialDbContext _context;

        public ProveedorService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Proveedore>> GetAllAsync()
        {
            return await _context.Proveedores
                .Where(p => !p.Baja??true)
                .OrderBy(p => p.NombreComercial)
                .ToListAsync();
        }

        public async Task<Proveedore?> GetByIdAsync(int id)
        {
            return await _context.Proveedores
            .FirstOrDefaultAsync(p => p.Id == id && (p.Baja == false));

        }

        /// <summary>
        /// Alta de proveedor con todos los campos
        /// </summary>
        public async Task<int> CreateAsync(
            string nombreComercial,
            string? cuil,
            string? direccion,
            string? email,
            string? telefono,
            string? celular,
            decimal ganancia,
            decimal descuento)
        {
            var nuevo = new Proveedore
            {
                NombreComercial = nombreComercial.Trim(),
                Cuil = cuil?.Trim(),
                Direccion = direccion?.Trim(),
                Email = email?.Trim(),
                Telefono = telefono?.Trim(),
                Celular = celular?.Trim(),
                Ganancia = ganancia,
                Descuento = descuento,
                Baja = false
            };

            _context.Proveedores.Add(nuevo);
            await _context.SaveChangesAsync();
            return nuevo.Id;
        }

        /// <summary>
        /// Modificación de proveedor con todos los campos
        /// </summary>
        public async Task UpdateAsync(
            int id,
            string nombreComercial,
            string? cuil,
            string? direccion,
            string? email,
            string? telefono,
            string? celular,
            decimal ganancia,
            decimal descuento)
        {
            var existente = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
            if (existente == null)
                throw new InvalidOperationException($"No se encontró el proveedor Id={id}");

            existente.NombreComercial = nombreComercial.Trim();
            existente.Cuil = cuil?.Trim();
            existente.Direccion = direccion?.Trim();
            existente.Email = email?.Trim();
            existente.Telefono = telefono?.Trim();
            existente.Celular = celular?.Trim();
            existente.Ganancia = ganancia;
            existente.Descuento = descuento;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Baja lógica
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var existente = await _context.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
            if (existente == null)
                return;

            existente.Baja = true;
            await _context.SaveChangesAsync();
        }
    }
}