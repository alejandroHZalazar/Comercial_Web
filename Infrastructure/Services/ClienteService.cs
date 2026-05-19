using Application.Interfaces;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using static Domain.DTO.ClienteDTO;

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

        public async Task<int> CreateAsync(Cliente unCliente)
        {
            var nuevo = new Cliente
            {
                NombreComercial = unCliente.NombreComercial,
                RazonSocial = unCliente.RazonSocial,
                Cuil = unCliente.Cuil,
                Direccion = unCliente.Direccion,
                Email = unCliente.Email,
                Telefono = unCliente.Telefono,
                Celular = unCliente.Celular,
                Contacto = unCliente.Contacto,
                FkCondIva = unCliente.FkCondIva,
                FkVendedor = unCliente.FkVendedor,
                Baja = false,
                FkLocalidad = unCliente.FkLocalidad,
                FkZona = unCliente.FkZona
                

            };
            _context.Clientes.Add(nuevo);
            await _context.SaveChangesAsync();
            return nuevo.Id;
        }

        public async Task UpdateAsync(Cliente unCliente)
        {
            var cliente = await _context.Clientes.FindAsync(unCliente.Id);
            if (cliente is null) return;

            cliente.NombreComercial = unCliente.NombreComercial;
            cliente.RazonSocial = unCliente.RazonSocial;
            cliente.Cuil = unCliente.Cuil;
            cliente.Direccion = unCliente.Direccion;
            cliente.Email = unCliente.Email;
            cliente.Telefono = unCliente.Telefono;
            cliente.Celular = unCliente.Celular;
            cliente.Contacto = unCliente.Contacto;
            cliente.FkCondIva = unCliente.FkCondIva;
            cliente.FkVendedor = unCliente.FkVendedor;
            cliente.Baja = false;
            cliente.FkLocalidad = unCliente.FkLocalidad;
            cliente.FkZona = unCliente.FkZona;
            await _context.SaveChangesAsync();
        }

        // ── Password ──────────────────────────────────────────────────────────
        public async Task<string> ResetPasswordAsync(int clienteId)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId)
                ?? throw new InvalidOperationException($"Cliente {clienteId} no encontrado.");

            var password = _GenerarPasswordSeguro();
            // Hashear con BCrypt vía ASP.NET Identity PasswordHasher (misma librería que usa el resto del sistema)
            cliente.PasswordHash = new PasswordHasher<object>().HashPassword(null!, password);
            await _context.SaveChangesAsync();

            return password;   // se devuelve solo una vez para mostrar al operador
        }

        /// <summary>
        /// Genera un password criptográficamente seguro de 12 caracteres:
        /// al menos 1 mayúscula, 1 minúscula, 1 dígito y 1 símbolo.
        /// </summary>
        private static string _GenerarPasswordSeguro()
        {
            const string mayus    = "ABCDEFGHJKLMNPQRSTUVWXYZ";   // sin I, O (confusos)
            const string minus    = "abcdefghjkmnpqrstuvwxyz";    // sin i, l, o
            const string digitos  = "23456789";                    // sin 0, 1
            const string simbolos = "!@#$%&*+-?";
            const string todos    = mayus + minus + digitos + simbolos;
            const int    longitud = 12;

            using var rng = RandomNumberGenerator.Create();
            var buf = new byte[longitud * 4];   // extra para descarte
            var chars = new char[longitud];

            // Garantizar al menos uno de cada categoría en posiciones fijas
            chars[0] = _RndChar(mayus,   rng);
            chars[1] = _RndChar(minus,   rng);
            chars[2] = _RndChar(digitos, rng);
            chars[3] = _RndChar(simbolos, rng);

            // Rellenar el resto aleatoriamente
            for (int i = 4; i < longitud; i++)
                chars[i] = _RndChar(todos, rng);

            // Mezclar para que los obligatorios no estén siempre al inicio
            rng.GetBytes(buf);
            for (int i = longitud - 1; i > 0; i--)
            {
                int j = (int)(BitConverter.ToUInt32(buf, i * 4) % (uint)(i + 1));
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        private static char _RndChar(string charset, RandomNumberGenerator rng)
        {
            var b = new byte[4];
            rng.GetBytes(b);
            return charset[(int)(BitConverter.ToUInt32(b, 0) % (uint)charset.Length)];
        }

        // ─────────────────────────────────────────────────────────────────────
        public async Task DeleteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente is null) return;

            cliente.Baja = true; // baja lógica
            await _context.SaveChangesAsync();
        }

        public async Task<List<Cliente>> BuscarAsync(int tipoBusqueda, string valor)
        {
            valor = valor?.ToLower() ?? "";

            IQueryable<Cliente> query = _context.Clientes;

            switch (tipoBusqueda)
            {
                case 0: // Nombre Comercial
                    query = query.Where(c =>
                        c.NombreComercial != null &&
                        c.NombreComercial.ToLower().Contains(valor) && c.Baja == false);
                    break;

                case 1: // Razón Social
                    query = query.Where(c =>
                        c.RazonSocial != null &&
                        c.RazonSocial.ToLower().Contains(valor) && c.Baja == false);
                    break;

                case 2: // CUIL
                    query = query.Where(c => c.Cuil == valor && c.Baja == false);
                    break;

                default:
                    query = query.Where(c => false);
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<List<Cliente>> BuscarPorLocalidadAsync(string valor)
        {         

            valor = valor?.ToLower() ?? "";

            var query =
                from c in _context.Clientes
                join l in _context.Localidades on c.FkLocalidad equals l.Id
                where l.Nombre.ToLower().Contains(valor) && c.Baja == false
                select c;

            return await query.ToListAsync();
        }

        public async Task<List<Cliente>> BuscarPorZonaAsync(string valor)
        {

            valor = valor?.ToLower() ?? "";

            var query =
                from c in _context.Clientes
                join z in _context.ClientesZonas on c.FkZona equals z.Id
                where z.Nombre.ToLower().Contains(valor) && c.Baja == false
                select c;

            return await query.ToListAsync();
        }

        public async Task<ClienteDetalleDTO> traerDetalleAsync(int id)
        {
            var query = await (
            from c in _context.Clientes
            join l in _context.Localidades on c.FkLocalidad equals l.Id into lg
            from l in lg.DefaultIfEmpty()
            join p in _context.Provincias on (int?)l.FkProvincia equals (int?)p.Id into pg
            from p in pg.DefaultIfEmpty()
            join z in _context.ClientesZonas on c.FkZona equals z.Id into zg
            from z in zg.DefaultIfEmpty()
            join i in _context.CondIvas on c.FkCondIva equals i.Id into ig
            from i in ig.DefaultIfEmpty()
            join u in _context.Usuarios on c.FkVendedor equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            where c.Id == id
            select new ClienteDetalleDTO
            {
                Id = c.Id,
                NombreComercial = c.NombreComercial,
                RazonSocial = c.RazonSocial,
                Cuil = c.Cuil,
                Direccion = c.Direccion,
                LocalidadDescripcion = l != null ? l.Nombre : null,
                ProvinciaDescripcion = p != null ? p.Nombre : null,
                ZonaDescripcion = z != null ? z.Nombre : null,
                Email = c.Email,
                Telefono = c.Telefono,
                Celular = c.Celular,
                Contacto = c.Contacto,
                CondicionIva = i != null ? i.Descripcion : null,
                Vendedor = u != null ? u.Nombre : null,
                FkCondIva    = c.FkCondIva,
                FkLocalidad  = c.FkLocalidad,
                FkVendedor   = c.FkVendedor,
                FkZona       = c.FkZona,
                PasswordHash = c.PasswordHash
            }).FirstOrDefaultAsync();

            return query;
        }

        public async Task<List<ClienteDetalleDTO>> GetAllConDetalleAsync()
        {
            return await (
                from c in _context.Clientes
                join l in _context.Localidades on c.FkLocalidad equals l.Id into lg
                from l in lg.DefaultIfEmpty()
                join p in _context.Provincias on (int?)l.FkProvincia equals (int?)p.Id into pg
                from p in pg.DefaultIfEmpty()
                join z in _context.ClientesZonas on c.FkZona equals z.Id into zg
                from z in zg.DefaultIfEmpty()
                join i in _context.CondIvas on c.FkCondIva equals i.Id into ig
                from i in ig.DefaultIfEmpty()
                join u in _context.Usuarios on c.FkVendedor equals u.Id into ug
                from u in ug.DefaultIfEmpty()
                where c.Baja != true
                orderby c.NombreComercial
                select new ClienteDetalleDTO
                {
                    Id = c.Id,
                    NombreComercial = c.NombreComercial,
                    RazonSocial = c.RazonSocial,
                    Cuil = c.Cuil,
                    Direccion = c.Direccion,
                    LocalidadDescripcion = l != null ? l.Nombre : null,
                    ProvinciaDescripcion  = p != null ? p.Nombre : null,
                    ZonaDescripcion      = z != null ? z.Nombre : null,
                    Email = c.Email,
                    Telefono = c.Telefono,
                    Celular = c.Celular,
                    Contacto = c.Contacto,
                    CondicionIva = i != null ? i.Descripcion : null,
                    Vendedor     = u != null ? u.Nombre : null,
                    FkCondIva    = c.FkCondIva,
                    FkLocalidad  = c.FkLocalidad,
                    FkVendedor   = c.FkVendedor,
                    FkZona       = c.FkZona
                }
            ).ToListAsync();
        }

    }
}