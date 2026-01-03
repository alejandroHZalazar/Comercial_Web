// Application/Services/VentasRankingService.cs
using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Services;
public class VentasRankingService : IVentasRankingService
{
    private readonly ComercialDbContext _context;

    public VentasRankingService(ComercialDbContext context)
    {
        _context = context;
    }

    public async Task<List<RankingItemDto>> TraerVentasRankingProductosAsync(DateTime desde, DateTime hasta)
    {
        var hastaInclusivo = hasta.Date.AddDays(1).AddTicks(-1);

        // VentasDetalle positivas
        var ventasQ =
            from d in _context.VentasDetalles
            join v in _context.Ventas on d.FkVenta equals v.Id
            where (v.TotalVenta ?? 0) > 0
               && v.Fecha >= desde && v.Fecha <= hastaInclusivo
            select new
            {
                Cod_Prov = d.CodProveedor,              // asumimos que el detalle expone estos campos
                Descripcion = d.Descripcion,
                Cantidad = d.Cantidad ?? 0m
            };

        // DevolucionesDetalle negativas
        var devolucionesQ =
            from dd in _context.DevolucionesDetalles
            join dev in _context.Devoluciones on dd.FkDevolucion equals dev.Id
            where dev.Fecha >= desde && dev.Fecha <= hastaInclusivo
            select new
            {
                Cod_Prov = dd.CodProveedor,
                Descripcion = dd.Descripcion,
                Cantidad = (dd.Cantidad ?? 0m) * -1m
            };

        var unionQ = ventasQ.Concat(devolucionesQ);

        var agrupado =
            from t in unionQ
            group t by new { t.Cod_Prov, t.Descripcion } into g
            select new RankingItemDto
            {
                Codigo = g.Key.Cod_Prov ?? "",
                Nombre = g.Key.Descripcion ?? "",
                Valor = g.Sum(x => x.Cantidad)
            };

        // En el SP se ordena por Cantidad (asc). Mantenemos ese criterio.
        return await agrupado
            .OrderByDescending(x => x.Valor)
            .ToListAsync();
    }

    public async Task<List<RankingItemDto>> TraerVentasRankingClientesAsync(DateTime desde, DateTime hasta)
    {
        var hastaInclusivo = hasta.Date.AddDays(1).AddTicks(-1);

        // Ventas (neto de IVA)
        var ventasQ =
            from v in _context.Ventas
            join c in _context.Clientes on v.FkCliente equals c.Id
            where v.Fecha >= desde && v.Fecha <= hastaInclusivo
            select new
            {
                Codigo = c.Id.ToString(),
                Nombre = c.NombreComercial,
                Ventas = (v.TotalVenta ?? 0m) / (1m + ((v.Iva ?? 0m) / 100m))
            };

        // Devoluciones (negativas, neto de IVA)
        var devolucionesQ =
            from d in _context.Devoluciones
            join c in _context.Clientes on d.FkCliente equals c.Id
            where d.Fecha >= desde && d.Fecha <= hastaInclusivo
            select new
            {
                Codigo = c.Id.ToString(),
                Nombre = c.NombreComercial,
                Ventas = ((d.TotalDevolucion ?? 0m) / (1m + ((d.Iva ?? 0m) / 100m))) * -1m
            };

        var unionQ = ventasQ.Concat(devolucionesQ);

        var agrupado =
            from t in unionQ
            group t by new { t.Codigo, t.Nombre } into g
            select new RankingItemDto
            {
                Codigo = g.Key.Codigo ?? "",
                Nombre = g.Key.Nombre ?? "",
                Valor = g.Sum(x => x.Ventas)
            };

        // Orden descendente y top 10 (como el SP)
        return await agrupado
            .OrderByDescending(x => x.Valor)
            .Take(10)
            .ToListAsync();
    }
}