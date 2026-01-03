using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ZonaClienteService : IZonaClienteService
    {
        private readonly ComercialDbContext _context;

        public ZonaClienteService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClientesZona>> GetAllAsync()
        {
            return await _context.ClientesZonas
                .Where(z => z.Baja != true)
                .OrderBy(z => z.Nombre)
                .ToListAsync();
        }

        public async Task<ClientesZona?> GetByIdAsync(int id)
        {
            return await _context.ClientesZonas
                .FirstOrDefaultAsync(z => z.Id == id && z.Baja != true);
        }

        public async Task CreateAsync(string nombre)
        {
            _context.ClientesZonas.Add(new ClientesZona { Nombre = nombre });
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, string nombre)
        {
            var zona = await _context.ClientesZonas.FindAsync(id);
            if (zona is null) return;

            zona.Nombre = nombre;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var zona = await _context.ClientesZonas.FindAsync(id);
            if (zona is null) return;

            zona.Baja = true;
            await _context.SaveChangesAsync();
        }
    }
}
