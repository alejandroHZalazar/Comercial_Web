using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Services
{
    public class ClienteService : IClienteService
    {
        private readonly ComercialDbContext _context;

        public ClienteService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> GetAllAsync()
        {
            return await _context.Clientes
                .Where(c => c.Baja != true)
                .OrderBy(c => c.NombreComercial)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdAsync(int id)
        {
            return await _context.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.Baja != true);
        }

        public async Task<int> CreateAsync(string nombreComercial)
        {
            var nuevo = new Cliente
            {
                NombreComercial = nombreComercial,
                Baja = false
            };
            _context.Clientes.Add(nuevo);
            await _context.SaveChangesAsync();
            return nuevo.Id;
        }

        public async Task UpdateAsync(int id, string nombreComercial)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente is null) return;

            cliente.NombreComercial = nombreComercial;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente is null) return;

            cliente.Baja = true; // baja lógica
            await _context.SaveChangesAsync();
        }
    }
}