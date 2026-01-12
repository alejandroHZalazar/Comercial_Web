using Application.Interfaces;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.Contracts;
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
            join l in _context.Localidades on c.FkLocalidad equals l.Id
            join p in _context.Provincias on l.FkProvincia equals p.Id
            join z in _context.ClientesZonas on c.FkZona equals z.Id
            join i in _context.CondIvas  on c.FkCondIva equals i.Id
            join u in _context.Usuarios on c.FkVendedor equals u.Id
            where c.Id == id
            select new ClienteDetalleDTO
            {
                Id = c.Id,
                NombreComercial = c.NombreComercial,
                RazonSocial = c.RazonSocial,
                Cuil = c.Cuil,
                Direccion = c.Direccion,
                LocalidadDescripcion = l.Nombre,
                ProvinciaDescripcion = p.Nombre,
                ZonaDescripcion = z.Nombre,
                Email = c.Email,
                Telefono = c.Telefono,
                Celular = c.Celular,
                Contacto = c.Contacto,
                CondicionIva = i.Descripcion,
                Vendedor = u.Nombre,
                FkCondIva = c.FkCondIva,
                FkLocalidad = c.FkLocalidad,
                FkVendedor = c.FkVendedor,
                FkZona = c.FkZona
            }).FirstOrDefaultAsync();

            return query;
        }

    }
}