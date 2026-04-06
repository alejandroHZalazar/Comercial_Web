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

    /// <summary>
    /// Ranking detallado de clientes equivalente al SP sp_Ventas_RankingClientes.
    /// Incluye: Total Sin IVA, Ticket Promedio, Cant Compras, Prod Distintos,
    /// % Participación, Costo, Rentabilidad, % Margen.
    /// Permite filtrar por múltiples proveedores.
    /// </summary>
    public async Task<List<RankingClienteDetalleDto>> TraerRankingClientesDetalleAsync(
        DateTime desde, DateTime hasta, List<int>? proveedorIds)
    {
        var desdeDate = desde.Date;
        var hastaExclusivo = hasta.Date.AddDays(1);

        var filtrarProveedor = proveedorIds != null && proveedorIds.Any();

        // ================= VENTAS =================
        var ventasQ =
            from d in _context.VentasDetalles
            join v in _context.Ventas on d.FkVenta equals v.Id
            join c in _context.Clientes on v.FkCliente equals c.Id
            join p in _context.Productos on d.FkProducto equals p.Id
            where v.Fecha >= desdeDate && v.Fecha < hastaExclusivo
               && (!filtrarProveedor || proveedorIds!.Contains(p.FkProveedor ?? 0))
            select new
            {
                ClienteId = c.Id,
                NomComercial = c.NombreComercial ?? "",
                IdComprobante = v.Id,
                FkProducto = d.FkProducto ?? 0,
                Total = d.SubtotalSinIva != null
                    ? d.SubtotalSinIva.Value * (d.Cantidad ?? 0m)
                    : (d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m)
                      - ((v.Descuento ?? 0m) / 100m * ((d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m)))
                      + ((v.Recargo ?? 0m) / 100m * ((d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m))),
                Costo = d.Costo ?? 0m
            };

        // ================= DEVOLUCIONES =================
        var devolucionesQ =
            from dd in _context.DevolucionesDetalles
            join dev in _context.Devoluciones on dd.FkDevolucion equals dev.Id
            join c in _context.Clientes on dev.FkCliente equals c.Id
            join p in _context.Productos on dd.FkProducto equals p.Id
            where dev.Fecha >= desdeDate && dev.Fecha < hastaExclusivo
               && (!filtrarProveedor || proveedorIds!.Contains(p.FkProveedor ?? 0))
            select new
            {
                ClienteId = c.Id,
                NomComercial = c.NombreComercial ?? "",
                IdComprobante = (long)dev.Id,
                FkProducto = dd.FkProducto ?? 0,
                Total = (dd.SubtotalSinIva != null
                    ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                    : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                      - ((dev.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)))
                      + ((dev.Recargo ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))))
                    * -1m,
                Costo = (dd.Costo ?? 0m) * -1m
            };

        // UNION ALL → materializar en memoria para agrupar con COUNT DISTINCT
        var union = await ventasQ.Concat(devolucionesQ).ToListAsync();

        // Agrupado por cliente
        var agrupado = union
            .GroupBy(x => new { x.ClienteId, x.NomComercial })
            .Select(g => new
            {
                g.Key.NomComercial,
                TotalVendido = g.Sum(x => x.Total),
                CostoTotal = g.Sum(x => x.Costo),
                CantCompras = g.Select(x => x.IdComprobante).Distinct().Count(),
                ProdDistintos = g.Select(x => x.FkProducto).Distinct().Count()
            })
            .ToList();

        var totalGeneral = agrupado.Sum(x => x.TotalVendido);

        return agrupado
            .OrderByDescending(a => a.TotalVendido)
            .Select(a => new RankingClienteDetalleDto
            {
                Cliente = a.NomComercial,
                TotalSinIva = Math.Round(a.TotalVendido, 2),
                TicketPromedio = a.CantCompras > 0
                    ? Math.Round(a.TotalVendido / a.CantCompras, 2) : 0,
                CantCompras = a.CantCompras,
                ProdDistintos = a.ProdDistintos,
                Participacion = totalGeneral != 0
                    ? Math.Round(a.TotalVendido / totalGeneral * 100m, 2) : 0,
                Costo = Math.Round(a.CostoTotal, 2),
                Rentabilidad = Math.Round(a.TotalVendido - a.CostoTotal, 2),
                Margen = a.TotalVendido != 0
                    ? Math.Round((a.TotalVendido - a.CostoTotal) / a.TotalVendido * 100m, 2) : 0
            })
            .ToList();
    }

    /// <summary>
    /// Ranking detallado de productos equivalente al SP sp_Ventas_RankingProductos.
    /// Incluye: Proveedor, Cod_Prov, Producto, Cantidad, Total Sin IVA, Precio Promedio,
    /// % Participación (por proveedor), Costo, Rentabilidad, Cantidad de Ventas.
    /// Permite filtrar por múltiples proveedores.
    /// </summary>
    public async Task<List<RankingProductoDetalleDto>> TraerRankingProductosDetalleAsync(
        DateTime desde, DateTime hasta, List<int>? proveedorIds)
    {
        var desdeDate = desde.Date;
        var hastaExclusivo = hasta.Date.AddDays(1);

        var filtrarProveedor = proveedorIds != null && proveedorIds.Any();

        // ================= VENTAS =================
        var ventasQ =
            from d in _context.VentasDetalles
            join v in _context.Ventas on d.FkVenta equals v.Id
            join p in _context.Productos on d.FkProducto equals p.Id
            where (v.TotalVenta ?? 0) > 0
               && v.Fecha >= desdeDate && v.Fecha < hastaExclusivo
               && (!filtrarProveedor || proveedorIds!.Contains(p.FkProveedor ?? 0))
            select new
            {
                ProveedorId = p.FkProveedor ?? 0,
                CodProv = d.CodProveedor ?? "",
                Descripcion = d.Descripcion ?? "",
                Cantidad = d.Cantidad ?? 0m,
                IdComprobante = v.Id,
                Total = d.SubtotalSinIva != null
                    ? d.SubtotalSinIva.Value * (d.Cantidad ?? 0m)
                    : (d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m)
                      - ((v.Descuento ?? 0m) / 100m * ((d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m)))
                      + ((v.Recargo ?? 0m) / 100m * ((d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m))),
                Costo = d.Costo ?? 0m
            };

        // ================= DEVOLUCIONES =================
        var devolucionesQ =
            from dd in _context.DevolucionesDetalles
            join dev in _context.Devoluciones on dd.FkDevolucion equals dev.Id
            join p in _context.Productos on dd.FkProducto equals p.Id
            where dev.Fecha >= desdeDate && dev.Fecha < hastaExclusivo
               && (!filtrarProveedor || proveedorIds!.Contains(p.FkProveedor ?? 0))
            select new
            {
                ProveedorId = p.FkProveedor ?? 0,
                CodProv = dd.CodProveedor ?? "",
                Descripcion = dd.Descripcion ?? "",
                Cantidad = (dd.Cantidad ?? 0m) * -1m,
                IdComprobante = (long)dev.Id,
                Total = (dd.SubtotalSinIva != null
                    ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                    : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                      - ((dev.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)))
                      + ((dev.Recargo ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))))
                    * -1m,
                Costo = (dd.Costo ?? 0m) * -1m
            };

        // UNION ALL → materializar para agrupar con COUNT DISTINCT
        var union = await ventasQ.Concat(devolucionesQ).ToListAsync();

        // Agrupado por Proveedor + CodProv + Descripcion
        var agrupado = union
            .GroupBy(x => new { x.ProveedorId, x.CodProv, x.Descripcion })
            .Select(g => new
            {
                g.Key.ProveedorId,
                g.Key.CodProv,
                g.Key.Descripcion,
                Cantidad = g.Sum(x => x.Cantidad),
                TotalVendido = g.Sum(x => x.Total),
                CostoTotal = g.Sum(x => x.Costo),
                CantidadVentas = g.Select(x => x.IdComprobante).Distinct().Count()
            })
            .ToList();

        // Totales por proveedor (para % participación)
        var totalesPorProveedor = agrupado
            .GroupBy(x => x.ProveedorId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalVendido));

        // Traer nombres de proveedores
        var proveedorIdsUsados = agrupado.Select(x => x.ProveedorId).Distinct().ToList();
        var proveedoresDict = await _context.Proveedores
            .Where(p => proveedorIdsUsados.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.NombreComercial ?? "");

        return agrupado
            .OrderByDescending(a => a.Cantidad)
            .Select(a =>
            {
                var totalProv = totalesPorProveedor.GetValueOrDefault(a.ProveedorId, 0m);
                return new RankingProductoDetalleDto
                {
                    Proveedor = proveedoresDict.GetValueOrDefault(a.ProveedorId, ""),
                    CodProv = a.CodProv,
                    Producto = a.Descripcion,
                    Cantidad = Math.Round(a.Cantidad, 0),
                    TotalSinIva = Math.Round(a.TotalVendido, 2),
                    PrecioPromedio = a.Cantidad != 0
                        ? Math.Round(a.TotalVendido / a.Cantidad, 2) : 0,
                    Participacion = totalProv != 0
                        ? Math.Round(a.TotalVendido / totalProv * 100m, 2) : 0,
                    Costo = Math.Round(a.CostoTotal, 2),
                    Rentabilidad = Math.Round(a.TotalVendido - a.CostoTotal, 2),
                    CantidadVentas = a.CantidadVentas
                };
            })
            .ToList();
    }
}