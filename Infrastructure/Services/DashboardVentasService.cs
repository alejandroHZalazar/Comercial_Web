using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DashboardVentasService : IDashboardVentasService
{
    private readonly ComercialDbContext _db;

    public DashboardVentasService(ComercialDbContext db) => _db = db;

    // =====================================================================
    // INDICADORES GLOBALES  (emula sp_Dashboard_Indicadores)
    // =====================================================================
    public async Task<DashboardIndicadoresDto> GetIndicadoresAsync(DateTime desde, DateTime hasta)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        // --- Ventas: línea a línea ---
        var ventasLineas = await (
            from v  in _db.Ventas
            where v.Fecha >= desdeDate && v.Fecha < hastaDate
            join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
            select new
            {
                VentaId = v.Id,
                Fecha   = v.Fecha,
                Total   = vd.SubtotalSinIva != null
                    ? vd.SubtotalSinIva.Value * (vd.Cantidad ?? 0m)
                    : (vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)
                      - (v.Descuento ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m))
                      + (v.Recargo   ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)),
                Costo = vd.Costo ?? 0m
            }
        ).ToListAsync();

        // Agrupar por cabecera de venta
        var ventasPorId = ventasLineas
            .GroupBy(x => new { x.VentaId, Fecha = x.Fecha!.Value.Date })
            .Select(g => new { g.Key.VentaId, g.Key.Fecha, Total = g.Sum(x => x.Total), Costo = g.Sum(x => x.Costo) })
            .ToList();

        // --- Devoluciones: línea a línea ---
        var devLineas = await (
            from d  in _db.Devoluciones
            where d.Fecha >= desdeDate && d.Fecha < hastaDate
            join dd in _db.DevolucionesDetalles on d.Id equals dd.FkDevolucion
            select new
            {
                VentaId = d.Id,
                Fecha   = d.Fecha,
                Total   = dd.SubtotalSinIva != null
                    ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                    : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                      - (d.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))
                      + (d.Recargo   ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)),
                Costo = dd.Costo ?? 0m
            }
        ).ToListAsync();

        var devPorId = devLineas
            .GroupBy(x => new { x.VentaId, Fecha = x.Fecha!.Value.Date })
            .Select(g => new { g.Key.VentaId, g.Key.Fecha, Total = g.Sum(x => x.Total), Costo = g.Sum(x => x.Costo) })
            .ToList();

        // --- Totales ---
        decimal totalVentas    = ventasPorId.Sum(x => x.Total) - devPorId.Sum(x => x.Total);
        int     cantidadVentas = ventasPorId.Select(x => x.VentaId).Distinct().Count();
        decimal costos         = ventasPorId.Sum(x => x.Costo)  - devPorId.Sum(x => x.Costo);
        decimal ganancias      = totalVentas - costos;
        decimal promedio       = cantidadVentas > 0 ? totalVentas / cantidadVentas : 0m;

        // --- Mejor día ---
        var porFecha = ventasPorId
            .Select(x => (x.Fecha, Total: x.Total))
            .Concat(devPorId.Select(x => (x.Fecha, Total: -x.Total)))
            .GroupBy(x => x.Fecha)
            .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        string mejorDia = porFecha != null
            ? $"{porFecha.Fecha:dd/MM/yyyy} — {porFecha.Total:N2}"
            : "-";

        // --- Compras (productosMovimientos tipoMovimiento=2) ---
        decimal compras = await _db.ProductosMovimientos
            .Where(pm => pm.TipoMovimiento == 2
                      && pm.FechaMov >= desdeDate
                      && pm.FechaMov < hastaDate)
            .SumAsync(pm => (pm.Cantidad ?? 0m) * (pm.Costo ?? 0m));

        return new DashboardIndicadoresDto
        {
            TotalVentas    = totalVentas,
            CantidadVentas = cantidadVentas,
            MejorDia       = mejorDia,
            Promedio       = promedio,
            Costos         = costos,
            Ganancias      = ganancias,
            Compras        = compras
        };
    }

    // =====================================================================
    // DATOS PARA GRAFICOS  (emula sp_Dashboard_Ventas)
    // =====================================================================
    public async Task<DashboardVentasDto> GetVentasAsync(DateTime desde, DateTime hasta)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        // --- Base línea: ventas ---
        var ventasLineas = await (
            from v  in _db.Ventas
            where v.Fecha >= desdeDate && v.Fecha < hastaDate
            join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
            select new
            {
                Fecha      = v.Fecha,
                FkCliente  = v.FkCliente,
                FkProducto = vd.FkProducto,
                Total      = vd.SubtotalSinIva != null
                    ? vd.SubtotalSinIva.Value * (vd.Cantidad ?? 0m)
                    : (vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)
                      - (v.Descuento ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m))
                      + (v.Recargo   ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m))
            }
        ).ToListAsync();

        // --- Base línea: devoluciones ---
        var devLineas = await (
            from d  in _db.Devoluciones
            where d.Fecha >= desdeDate && d.Fecha < hastaDate
            join dd in _db.DevolucionesDetalles on d.Id equals dd.FkDevolucion
            select new
            {
                Fecha      = d.Fecha,
                FkCliente  = d.FkCliente,
                FkProducto = dd.FkProducto,
                Total      = dd.SubtotalSinIva != null
                    ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                    : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                      - (d.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))
                      + (d.Recargo   ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))
            }
        ).ToListAsync();

        // UNION ALL en memoria (devoluciones con total negativo)
        var todasLineas = ventasLineas
            .Select(x => (x.Fecha, x.FkCliente, x.FkProducto, Total: x.Total))
            .Concat(devLineas.Select(x => (x.Fecha, x.FkCliente, x.FkProducto, Total: -x.Total)))
            .ToList();

        // ---- 1. VENTAS POR DÍA ----
        var ventasPorDia = todasLineas
            .Where(x => x.Fecha.HasValue)
            .GroupBy(x => x.Fecha!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardSerieDto
            {
                Label = g.Key.ToString("dd/MM"),
                Value = g.Sum(x => x.Total)
            })
            .ToList();

        // ---- 2. VENTAS POR CLIENTE (top 10) ----
        var clienteIds = todasLineas
            .Where(x => x.FkCliente.HasValue)
            .Select(x => x.FkCliente!.Value)
            .Distinct().ToList();

        var clientesDict = clienteIds.Any()
            ? await _db.Clientes
                .Where(c => clienteIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? "Sin Nombre")
            : new Dictionary<int, string>();

        var ventasPorCliente = todasLineas
            .Where(x => x.FkCliente.HasValue)
            .GroupBy(x => x.FkCliente!.Value)
            .Select(g => new DashboardSerieDto
            {
                Label = clientesDict.TryGetValue(g.Key, out var nc) ? nc : "?",
                Value = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        // ---- 3. MEDIOS DE PAGO (solo ventas, usando totalVenta almacenado) ----
        var ventasRaw = await _db.Ventas
            .Where(v => v.Fecha >= desdeDate && v.Fecha < hastaDate)
            .Select(v => new
            {
                Id          = (int)v.Id,
                TotalSinIva = (v.TotalVenta ?? 0m) / (1m + (v.Iva ?? 0m) / 100m)
            })
            .ToListAsync();

        var ventaIdsList = ventasRaw.Select(v => v.Id).ToList();
        var formasPago   = ventaIdsList.Any()
            ? await _db.VentasFormasPago
                .Where(vfp => ventaIdsList.Contains(vfp.FkVenta))
                .ToListAsync()
            : new List<VentasFormasPago>();

        var mediosPagoNombres = await _db.MediosPago
            .ToDictionaryAsync(mp => mp.Id, mp => mp.Nombre);

        var ventaTotalDict       = ventasRaw.ToDictionary(v => v.Id, v => v.TotalSinIva);
        var ventasConFormaPago   = new HashSet<int>(formasPago.Select(vfp => vfp.FkVenta));

        var mediosPagoLista = formasPago
            .GroupBy(vfp => mediosPagoNombres.TryGetValue(vfp.FkMedioPago, out var n) ? n : "Sin Especificar")
            .Select(g => new DashboardSerieDto
            {
                Label = g.Key,
                Value = g.Sum(vfp => ventaTotalDict.TryGetValue(vfp.FkVenta, out var t) ? t : 0m)
            })
            .ToList();

        // Ventas sin forma de pago → "Sin Especificar"
        decimal sinEspTotal = ventasRaw
            .Where(v => !ventasConFormaPago.Contains(v.Id))
            .Sum(v => v.TotalSinIva);

        if (sinEspTotal > 0)
        {
            var sinEsp = mediosPagoLista.FirstOrDefault(m => m.Label == "Sin Especificar");
            if (sinEsp != null) sinEsp.Value += sinEspTotal;
            else mediosPagoLista.Add(new DashboardSerieDto { Label = "Sin Especificar", Value = sinEspTotal });
        }

        var mediosPago = mediosPagoLista.OrderByDescending(x => x.Value).ToList();

        // ---- 4 & 5. VENTAS POR PROVEEDOR y RUBRO (via Producto) ----
        var productoIds = todasLineas
            .Where(x => x.FkProducto.HasValue)
            .Select(x => x.FkProducto!.Value)
            .Distinct().ToList();

        var productosData = productoIds.Any()
            ? await _db.Productos
                .Where(p => productoIds.Contains(p.Id))
                .Select(p => new { p.Id, p.FkProveedor, p.FkRubro })
                .ToListAsync()
            : new List<(int Id, int? FkProveedor, int? FkRubro)>()
                .Select(t => new { Id = t.Id, FkProveedor = t.FkProveedor, FkRubro = t.FkRubro })
                .ToList();

        var productoProveedorDict = productosData
            .Where(p => p.FkProveedor.HasValue)
            .ToDictionary(p => p.Id, p => p.FkProveedor!.Value);

        var productoRubroDict = productosData
            .Where(p => p.FkRubro.HasValue)
            .ToDictionary(p => p.Id, p => p.FkRubro!.Value);

        var proveedorIds = productoProveedorDict.Values.Distinct().ToList();
        var proveedoresDict = proveedorIds.Any()
            ? await _db.Proveedores
                .Where(p => proveedorIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.NombreComercial ?? "?")
            : new Dictionary<int, string>();

        var rubroIds = productoRubroDict.Values.Distinct().ToList();
        var rubrosDict = rubroIds.Any()
            ? await _db.Rubros
                .Where(r => rubroIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Descripcion ?? "?")
            : new Dictionary<int, string>();

        var ventasPorProveedor = todasLineas
            .Where(x => x.FkProducto.HasValue && productoProveedorDict.ContainsKey(x.FkProducto.Value))
            .GroupBy(x => productoProveedorDict[x.FkProducto!.Value])
            .Select(g => new DashboardSerieDto
            {
                Label = proveedoresDict.TryGetValue(g.Key, out var pn) ? pn : "?",
                Value = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var ventasPorRubro = todasLineas
            .Where(x => x.FkProducto.HasValue && productoRubroDict.ContainsKey(x.FkProducto.Value))
            .GroupBy(x => productoRubroDict[x.FkProducto!.Value])
            .Select(g => new DashboardSerieDto
            {
                Label = rubrosDict.TryGetValue(g.Key, out var rn) ? rn : "Sin Rubro",
                Value = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return new DashboardVentasDto
        {
            VentasPorDia       = ventasPorDia,
            VentasPorCliente   = ventasPorCliente,
            MediosPago         = mediosPago,
            VentasPorProveedor = ventasPorProveedor,
            VentasPorRubro     = ventasPorRubro
        };
    }
}
