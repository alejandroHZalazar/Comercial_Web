using Domain.Contracts;
using Domain.Entities;
using System.Text;
using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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
        var hashed = HashPassword(password);
        var nuevo = new Usuario
        {
            Nombre = nombre,
            Password = hashed,
            Tipo = tipoUsuarioId,
            Baja = false,
            PasswordHash = hashed,
            PasswordMigrated = true
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
        {
            var hashed = HashPassword(password);
            existente.Password = hashed;
            existente.PasswordHash = hashed;
            existente.PasswordMigrated = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return;
        usuario.Baja = true; // baja lógica
        await _context.SaveChangesAsync();
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}