using Domain.Contracts;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Services{   

    public class MenuPermisoService : IMenuPermisoService
    {
        private readonly ComercialDbContext _context;

        public MenuPermisoService(ComercialDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuPermiso>> GetAllAsync()
        {
            return await _context.MenuPermisos
                                 .OrderBy(m => m.Funcion)
                                 .ToListAsync();
        }

        public async Task<List<int>> GetPermisosPorTipoUsuarioAsync(int tipoUsuarioId)
        {
            return await _context.TipoDeUsuariosPermisos
                                 .Where(p => p.FkTipoUsuario == tipoUsuarioId)
                                 .Select(p => p.FkMenuPermiso.Value)
                                 .ToListAsync();
        }
    }
}