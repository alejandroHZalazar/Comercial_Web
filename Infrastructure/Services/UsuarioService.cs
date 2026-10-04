using Domain.Contracts;
using Domain.Entities;
using System.Text;
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using static Domain.DTO.UsuarioDTO;

public class UsuarioService : IUsuarioService
{
    private readonly ComercialDbContext _context;

    public UsuarioService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<Usuario>> GetAllAsync()
    {
        return await _context.Usuarios.Where(u => u.Baja != true).ToListAsync();
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public async Task<int> CreateAsync(string nombre, string password, int tipoUsuarioId)
    {
        var nuevo = new Usuario
        {
            Nombre = nombre,
            Password = password,
            Tipo = tipoUsuarioId,
            Baja = false
        };
        _context.Usuarios.Add(nuevo);
        await _context.SaveChangesAsync();
        return nuevo.Id;
    }

    public async Task UpdateAsync(int id, string nombre, int tipoUsuarioId, string? password = null)
    {
        var existente = await _context.Usuarios.FindAsync(id);
        if (existente is null) return;

        existente.Nombre = nombre;
        existente.Tipo = tipoUsuarioId;

        if (!string.IsNullOrEmpty(password))
            existente.Password = password;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return;
        usuario.Baja = true; // baja lógica
        await _context.SaveChangesAsync();
    }

    public async Task<List<UsuarioGridABMItem>> GetUsuarioGrillaABM()
    {
        var query = await (
            from u in _context.Usuarios
            join t in _context.TipoUsuarios on u.Tipo equals t.Id
            where u.Baja == false
            select new UsuarioGridABMItem
            {
                Id = u.Id,
                Nombre = u.Nombre,
                TipoDescripcion = t.Descripcion
            }).ToListAsync();

        return query;
    }
}