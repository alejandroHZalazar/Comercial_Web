using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class VentasEstadisticasService : IVentasEstadisticasService
{
    private readonly ComercialDbContext _context;

    public VentasEstadisticasService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<VentaResumenDto>> GetResumenVentasAsync(
    DateTime desde, DateTime hasta, int? clienteId, int? vendedorId, int? proveedorId)
    {
        var baseQuery =
            from v in _context.Ventas
            join c in _context.Clientes on v.FkCliente equals c.Id into cli
            from c in cli.DefaultIfEmpty()
            join u in _context.Usuarios on v.FkVendedor equals u.Id into ven
            from u in ven.DefaultIfEmpty()
            where v.Fecha >= desde && v.Fecha <= hasta
            select new { v, c, u };

        if (clienteId.HasValue)
            baseQuery = baseQuery.Where(x => x.v.FkCliente == clienteId.Value);

        if (vendedorId.HasValue)
            baseQuery = baseQuery.Where(x => x.v.FkVendedor == vendedorId.Value);

        // Filtro por proveedor: ventas que tienen al menos un detalle con producto del proveedor
        if (proveedorId.HasValue)
        {
            baseQuery =
                from x in baseQuery
                where
                    (from d in _context.VentasDetalles
                     join p in _context.Productos on d.FkProducto equals p.Id
                     where d.FkVenta == x.v.Id && p.FkProveedor == proveedorId.Value
                     select d.FkVenta).Any()
                select x;
        }

        return await baseQuery
            .Select(x => new VentaResumenDto
            {
                Nro = x.v.Id,
                Fecha = x.v.Fecha ?? DateTime.MinValue,
                Cliente = x.c != null ? x.c.NombreComercial : "",
                Vendedor = x.u != null ? x.u.Nombre : "",
                IVA = x.v.Iva ?? 0,
                Desc_Rec = (x.v.Descuento ?? 0) * -1 + (x.v.Recargo ?? 0),
                TotalCIVA = x.v.TotalVenta ?? 0,
                totalSIVA = (x.v.TotalVenta ?? 0) / (1 + ((x.v.Iva ?? 0) / 100)),
                Costo = x.v.TotalCosto ?? 0,
                P_Com = (x.v.Comision ?? 0) * 100,
                Comision = ((x.v.TotalVenta ?? 0) / (1 + ((x.v.Iva ?? 0) / 100))) * (x.v.Comision ?? 0)
            })
            .OrderBy(x => x.Fecha)
            .ToListAsync();
    }

    public async Task<List<VentaDetalleDto>> GetDetalleVentaAsync(long ventaId)
    {
        var query =
        from d in _context.VentasDetalles
        where d.FkVenta == ventaId
        select new VentaDetalleDto
        {
            CodProv = d.CodProveedor ?? "",
            CodBarras = d.CodBarras ?? "",
            Descripcion = d.Descripcion ?? "",
            PrecioSIVA = d.PrecioSinIva??0,
            Cantidad = d.Cantidad ?? 0,
            Subtotal = (d.PrecioSinIva ?? 0) * (d.Cantidad ?? 0)

        };

        return await query.ToListAsync();

    }
}