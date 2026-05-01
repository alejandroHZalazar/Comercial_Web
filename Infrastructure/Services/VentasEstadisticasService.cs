using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        // Incluir el día completo de "hasta"
        var hastaFin = hasta.Date.AddDays(1);

        // ── Filtro de productos por proveedor (se reutiliza en ambas ramas) ──────
        HashSet<int>? prodIdsDelProv = null;
        if (proveedorId.HasValue)
        {
            prodIdsDelProv = (await _context.Productos
                .Where(p => p.FkProveedor == proveedorId.Value)
                .Select(p => p.Id)
                .ToListAsync())
                .ToHashSet();
        }

        // ── 1. Ventas ────────────────────────────────────────────────────────────
        // Inner join: ventas → ventasDetalle → clientes → usuarios
        // Emula SP tipo=1, rama ventas
        var ventasJoin = await (
            from v  in _context.Ventas
            join vd in _context.VentasDetalles on v.Id equals vd.FkVenta        // INNER JOIN
            join c  in _context.Clientes       on v.FkCliente equals (int?)c.Id // INNER JOIN
            join u  in _context.Usuarios       on v.FkVendedor equals (int?)u.Id // INNER JOIN
            where v.Fecha >= desde && v.Fecha < hastaFin
            select new
            {
                VId            = v.Id,
                VFecha         = v.Fecha,
                VTotalCosto    = v.TotalCosto,
                VNombreCliente = c.NombreComercial,
                VNombreVend    = u.Nombre,
                VIva           = v.Iva,
                VImpuesto      = v.Impuesto,
                VDescuento     = v.Descuento,
                VRecargo       = v.Recargo,
                VTotalVenta    = v.TotalVenta,
                VComision      = v.Comision,
                VFkCliente     = v.FkCliente,
                VFkVendedor    = v.FkVendedor,
                DSubSIva       = vd.SubtotalSinIva,
                DPrecioSIva    = vd.PrecioSinIva,
                DCantidad      = vd.Cantidad,
                DFkProducto    = vd.FkProducto
            }
        ).ToListAsync();

        // Filtros en memoria
        if (clienteId.HasValue)
            ventasJoin = ventasJoin.Where(x => x.VFkCliente == clienteId.Value).ToList();
        if (vendedorId.HasValue)
            ventasJoin = ventasJoin.Where(x => x.VFkVendedor == vendedorId.Value).ToList();
        if (prodIdsDelProv != null)
        {
            // Conservar las filas de ventas que tengan AL MENOS UNA línea del proveedor
            var ventaIdsConProv = ventasJoin
                .Where(x => x.DFkProducto.HasValue && prodIdsDelProv.Contains(x.DFkProducto.Value))
                .Select(x => x.VId)
                .ToHashSet();
            ventasJoin = ventasJoin.Where(x => ventaIdsConProv.Contains(x.VId)).ToList();
        }

        var ventasResult = ventasJoin
            .GroupBy(x => new
            {
                x.VId, x.VFecha, x.VTotalCosto,
                x.VNombreCliente, x.VNombreVend,
                x.VIva, x.VImpuesto, x.VDescuento, x.VRecargo,
                x.VTotalVenta, x.VComision
            })
            .Select(g =>
            {
                decimal totalSinIva = g.Sum(x =>
                    (x.DSubSIva ?? x.DPrecioSIva ?? 0m) * (x.DCantidad ?? 0m));
                decimal iva     = g.Key.VIva     ?? 0m;
                decimal total   = g.Key.VTotalVenta ?? 0m;
                decimal comPct  = g.Key.VComision   ?? 0m;

                return new VentaResumenDto
                {
                    Nro          = g.Key.VId,
                    Fecha        = g.Key.VFecha        ?? DateTime.MinValue,
                    Costo        = g.Key.VTotalCosto   ?? 0m,
                    Cliente      = g.Key.VNombreCliente ?? "",
                    Vendedor     = g.Key.VNombreVend    ?? "",
                    IVA          = iva,
                    Impuesto     = g.Key.VImpuesto      ?? 0m,
                    Desc_Rec     = (g.Key.VDescuento ?? 0m) * -1m + (g.Key.VRecargo ?? 0m),
                    TotalCIVA    = total,
                    totalSIVA    = totalSinIva,
                    P_Com        = comPct * 100m,
                    Comision     = iva > 0
                                   ? (total / (1m + iva / 100m)) * comPct
                                   : total * comPct,
                    EsDevolucion = false
                };
            })
            .ToList();

        // ── 2. Devoluciones ──────────────────────────────────────────────────────
        // Inner join: devoluciones → devolucionesDetalles → clientes → usuarios
        // Emula SP tipo=1, rama devoluciones (montos negados)
        var devsJoin = await (
            from d  in _context.Devoluciones
            join dd in _context.DevolucionesDetalles on (int?)d.Id equals dd.FkDevolucion // INNER JOIN
            join c  in _context.Clientes             on d.FkCliente equals (int?)c.Id     // INNER JOIN
            join u  in _context.Usuarios             on d.FkVendedor equals (int?)u.Id    // INNER JOIN
            where d.Fecha >= desde && d.Fecha < hastaFin
            select new
            {
                DId            = d.Id,
                DFecha         = d.Fecha,
                DTotalCosto    = d.TotalCosto,
                DNombreCliente = c.NombreComercial,
                DNombreVend    = u.Nombre,
                DIva           = d.Iva,
                DImpuesto      = d.Impuesto,
                DDescuento     = d.Descuento,
                DRecargo       = d.Recargo,
                DTotalDev      = d.TotalDevolucion,
                DComision      = d.Comision,
                DFkCliente     = d.FkCliente,
                DFkVendedor    = d.FkVendedor,
                DDSubSIva      = dd.SubtotalSinIva,
                DDPrecioSIva   = dd.PrecioSinIva,
                DDCantidad     = dd.Cantidad,
                DDFkProducto   = dd.FkProducto
            }
        ).ToListAsync();

        // Filtros en memoria
        if (clienteId.HasValue)
            devsJoin = devsJoin.Where(x => x.DFkCliente == clienteId.Value).ToList();
        if (vendedorId.HasValue)
            devsJoin = devsJoin.Where(x => x.DFkVendedor == vendedorId.Value).ToList();
        if (prodIdsDelProv != null)
        {
            var devIdsConProv = devsJoin
                .Where(x => x.DDFkProducto.HasValue && prodIdsDelProv.Contains(x.DDFkProducto.Value))
                .Select(x => x.DId)
                .ToHashSet();
            devsJoin = devsJoin.Where(x => devIdsConProv.Contains(x.DId)).ToList();
        }

        var devsResult = devsJoin
            .GroupBy(x => new
            {
                x.DId, x.DFecha, x.DTotalCosto,
                x.DNombreCliente, x.DNombreVend,
                x.DIva, x.DImpuesto, x.DDescuento, x.DRecargo,
                x.DTotalDev, x.DComision
            })
            .Select(g =>
            {
                decimal totalSinIva = g.Sum(x =>
                    (x.DDSubSIva ?? x.DDPrecioSIva ?? 0m) * (x.DDCantidad ?? 0m));
                decimal iva    = g.Key.DIva    ?? 0m;
                decimal total  = g.Key.DTotalDev ?? 0m;
                decimal comPct = g.Key.DComision  ?? 0m;

                return new VentaResumenDto
                {
                    Nro          = g.Key.DId,
                    Fecha        = g.Key.DFecha        ?? DateTime.MinValue,
                    Costo        = (g.Key.DTotalCosto   ?? 0m) * -1m,
                    Cliente      = g.Key.DNombreCliente ?? "",
                    Vendedor     = g.Key.DNombreVend    ?? "",
                    IVA          = iva,
                    Impuesto     = g.Key.DImpuesto      ?? 0m,
                    Desc_Rec     = (g.Key.DDescuento ?? 0m) * -1m + (g.Key.DRecargo ?? 0m),
                    TotalCIVA    = total * -1m,
                    totalSIVA    = totalSinIva * -1m,
                    P_Com        = comPct * 100m,
                    Comision     = iva > 0
                                   ? (total / (1m + iva / 100m)) * comPct * -1m
                                   : total * comPct * -1m,
                    EsDevolucion = true
                };
            })
            .ToList();

        // ── UNION ALL ordenado por fecha ─────────────────────────────────────────
        return ventasResult
            .Concat(devsResult)
            .OrderBy(x => x.Fecha)
            .ToList();
    }

    public async Task<List<VentaDetalleDto>> GetDetalleVentaAsync(long ventaId)
    {
        return await (
            from d in _context.VentasDetalles
            where d.FkVenta == ventaId
            select new VentaDetalleDto
            {
                CodProv     = d.CodProveedor ?? "",
                CodBarras   = d.CodBarras    ?? "",
                Descripcion = d.Descripcion  ?? "",
                PrecioSIVA  = d.PrecioSinIva ?? 0m,
                Cantidad    = d.Cantidad     ?? 0m,
                Subtotal    = (d.PrecioSinIva ?? 0m) * (d.Cantidad ?? 0m)
            }
        ).ToListAsync();
    }

    public async Task<List<VentaDetalleDto>> GetDetalleDevolucionAsync(long devolucionId)
    {
        return await (
            from dd in _context.DevolucionesDetalles
            where dd.FkDevolucion == (int)devolucionId
            select new VentaDetalleDto
            {
                CodProv     = dd.CodProveedor ?? "",
                CodBarras   = dd.CodBarras    ?? "",
                Descripcion = dd.Descripcion  ?? "",
                PrecioSIVA  = dd.PrecioSinIva ?? 0m,
                Cantidad    = dd.Cantidad     ?? 0m,
                Subtotal    = (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
            }
        ).ToListAsync();
    }
}
