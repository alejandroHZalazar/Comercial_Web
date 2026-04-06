using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DashboardClienteService : IDashboardClienteService
{
    private readonly ComercialDbContext _db;

    public DashboardClienteService(ComercialDbContext db) => _db = db;

    // =====================================================================
    // KPIs GLOBALES  (emula sp_Dashboard_ClientesMetricas)
    // =====================================================================
    public async Task<ClienteMetricasDto> GetMetricasAsync(
        DateTime   desde,
        DateTime   hasta,
        List<int>? clienteIds)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        // ---- Ventas línea a línea ----
        var qVentas = from v  in _db.Ventas
                      where v.Fecha >= desdeDate && v.Fecha < hastaDate
                      join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
                      select new
                      {
                          VentaId   = v.Id,
                          Fecha     = v.Fecha,
                          FkCliente = v.FkCliente,
                          Total     = vd.SubtotalSinIva != null
                              ? vd.SubtotalSinIva.Value * (vd.Cantidad ?? 0m)
                              : (vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)
                                - (v.Descuento ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m))
                                + (v.Recargo   ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)),
                          Costo = vd.Costo ?? 0m
                      };

        var ventasLineas = await qVentas.ToListAsync();

        // Filtro cliente en memoria (más simple que LINQ dinámico con Contains)
        if (clienteIds != null && clienteIds.Any())
            ventasLineas = ventasLineas.Where(x => x.FkCliente.HasValue && clienteIds.Contains(x.FkCliente.Value)).ToList();

        var ventasPorId = ventasLineas
            .GroupBy(x => new { x.VentaId, Fecha = x.Fecha!.Value.Date })
            .Select(g => new { g.Key.VentaId, g.Key.Fecha, Total = g.Sum(x => x.Total), Costo = g.Sum(x => x.Costo) })
            .ToList();

        // ---- Devoluciones línea a línea ----
        var qDev = from d  in _db.Devoluciones
                   where d.Fecha >= desdeDate && d.Fecha < hastaDate
                   join dd in _db.DevolucionesDetalles on d.Id equals dd.FkDevolucion
                   select new
                   {
                       VentaId   = d.Id,
                       Fecha     = d.Fecha,
                       FkCliente = d.FkCliente,
                       Total     = dd.SubtotalSinIva != null
                           ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                           : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                             - (d.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))
                             + (d.Recargo   ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)),
                       Costo = dd.Costo ?? 0m
                   };

        var devLineas = await qDev.ToListAsync();

        if (clienteIds != null && clienteIds.Any())
            devLineas = devLineas.Where(x => x.FkCliente.HasValue && clienteIds.Contains(x.FkCliente.Value)).ToList();

        var devPorId = devLineas
            .GroupBy(x => new { x.VentaId, Fecha = x.Fecha!.Value.Date })
            .Select(g => new { g.Key.VentaId, g.Key.Fecha, Total = g.Sum(x => x.Total), Costo = g.Sum(x => x.Costo) })
            .ToList();

        // ---- Totales ----
        decimal totalVentas    = ventasPorId.Sum(x => x.Total) - devPorId.Sum(x => x.Total);
        int     cantidadVentas = ventasPorId.Select(x => x.VentaId).Distinct().Count();
        decimal costos         = ventasPorId.Sum(x => x.Costo)  - devPorId.Sum(x => x.Costo);
        decimal ganancias      = totalVentas - costos;
        decimal promedio       = cantidadVentas > 0 ? totalVentas / cantidadVentas : 0m;

        // ---- Mejor día ----
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

        return new ClienteMetricasDto
        {
            TotalVentas    = totalVentas,
            CantidadVentas = cantidadVentas,
            MejorDia       = mejorDia,
            Promedio       = promedio,
            Costos         = costos,
            Ganancias      = ganancias
        };
    }

    // =====================================================================
    // GRÁFICOS  (emula sp_Dashboard_ClientesGraficos — sin MovimientosCC)
    // =====================================================================
    public async Task<ClienteGraficosDto> GetGraficosAsync(
        DateTime   desde,
        DateTime   hasta,
        List<int>? clienteIds)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        // ---- Base ventas ----
        var ventasLineas = await (
            from v  in _db.Ventas
            where v.Fecha >= desdeDate && v.Fecha < hastaDate
            join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
            select new
            {
                VentaId    = v.Id,
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

        if (clienteIds != null && clienteIds.Any())
            ventasLineas = ventasLineas.Where(x => x.FkCliente.HasValue && clienteIds.Contains(x.FkCliente.Value)).ToList();

        // ---- Base devoluciones ----
        var devLineas = await (
            from d  in _db.Devoluciones
            where d.Fecha >= desdeDate && d.Fecha < hastaDate
            join dd in _db.DevolucionesDetalles on d.Id equals dd.FkDevolucion
            select new
            {
                VentaId    = d.Id,
                Fecha      = d.Fecha,
                FkCliente  = d.FkCliente,
                FkProducto = dd.FkProducto,
                Total      = (dd.SubtotalSinIva != null
                    ? dd.SubtotalSinIva.Value * (dd.Cantidad ?? 0m)
                    : (dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m)
                      - (d.Descuento ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))
                      + (d.Recargo   ?? 0m) / 100m * ((dd.PrecioSinIva ?? 0m) * (dd.Cantidad ?? 0m))) * -1m
            }
        ).ToListAsync();

        if (clienteIds != null && clienteIds.Any())
            devLineas = devLineas.Where(x => x.FkCliente.HasValue && clienteIds.Contains(x.FkCliente.Value)).ToList();

        // UNION ALL en memoria
        var todasLineas = ventasLineas
            .Select(x => (x.Fecha, x.FkCliente, x.FkProducto, x.Total))
            .Concat(devLineas.Select(x => (x.Fecha, x.FkCliente, x.FkProducto, x.Total)))
            .ToList();

        // =================================================================
        // 1. CURVAS — Ventas por Día por Cliente (top 10 clientes)
        // =================================================================
        var clienteIdsEnBase = todasLineas
            .Where(x => x.FkCliente.HasValue)
            .Select(x => x.FkCliente!.Value)
            .Distinct().ToList();

        var clientesDict = clienteIdsEnBase.Any()
            ? await _db.Clientes
                .Where(c => clienteIdsEnBase.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? "Sin Nombre")
            : new Dictionary<int, string>();

        // Top 10 clientes por total general
        var top10Ids = todasLineas
            .Where(x => x.FkCliente.HasValue)
            .GroupBy(x => x.FkCliente!.Value)
            .Select(g => new { ClienteId = g.Key, Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .Select(x => x.ClienteId)
            .ToHashSet();

        // Fechas distintas ordenadas
        var allDates = todasLineas
            .Where(x => x.Fecha.HasValue)
            .Select(x => x.Fecha!.Value.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        // Agrupación base: por fecha × cliente (solo top10)
        var baseAgrupada = todasLineas
            .Where(x => x.Fecha.HasValue && x.FkCliente.HasValue && top10Ids.Contains(x.FkCliente.Value))
            .GroupBy(x => new { Fecha = x.Fecha!.Value.Date, ClienteId = x.FkCliente!.Value })
            .ToDictionary(g => (g.Key.Fecha, g.Key.ClienteId), g => g.Sum(x => x.Total));

        var curvasLabels  = allDates.Select(d => d.ToString("dd/MM")).ToList();
        var curvasSeries  = top10Ids
            .OrderByDescending(cid => todasLineas
                .Where(x => x.FkCliente == cid)
                .Sum(x => x.Total))
            .Select(cid => new CurvaSerieDto
            {
                Label = clientesDict.TryGetValue(cid, out var nombre) ? nombre : "?",
                Data  = allDates
                    .Select(d => baseAgrupada.TryGetValue((d, cid), out var v) ? v : 0m)
                    .ToList()
            })
            .ToList();

        // =================================================================
        // 2. MEDIOS DE PAGO — solo ventas, usando totalVenta almacenado
        // =================================================================
        var ventaIdsEnBase = ventasLineas
            .Select(x => x.VentaId)
            .Distinct().ToList();

        List<DashboardSerieDto> mediosPagoLista = new();

        if (ventaIdsEnBase.Any())
        {
            var ventasRaw = await _db.Ventas
                .Where(v => ventaIdsEnBase.Contains((int)v.Id))
                .Select(v => new
                {
                    Id          = (int)v.Id,
                    TotalSinIva = (v.TotalVenta ?? 0m) / (1m + (v.Iva ?? 0m) / 100m)
                })
                .ToListAsync();

            var formasPago = await _db.VentasFormasPago
                .Where(vfp => ventaIdsEnBase.Contains(vfp.FkVenta))
                .ToListAsync();

            var mediosPagoNombres = await _db.MediosPago
                .ToDictionaryAsync(mp => mp.Id, mp => mp.Nombre);

            var ventaTotalDict     = ventasRaw.ToDictionary(v => v.Id, v => v.TotalSinIva);
            var ventasConFormaPago = new HashSet<int>(formasPago.Select(vfp => vfp.FkVenta));

            mediosPagoLista = formasPago
                .GroupBy(vfp => mediosPagoNombres.TryGetValue(vfp.FkMedioPago, out var n) ? n : "Sin Especificar")
                .Select(g => new DashboardSerieDto
                {
                    Label = g.Key,
                    Value = g.Sum(vfp => ventaTotalDict.TryGetValue(vfp.FkVenta, out var t) ? t : 0m)
                })
                .ToList();

            decimal sinEspTotal = ventasRaw
                .Where(v => !ventasConFormaPago.Contains(v.Id))
                .Sum(v => v.TotalSinIva);

            if (sinEspTotal > 0)
            {
                var sinEsp = mediosPagoLista.FirstOrDefault(m => m.Label == "Sin Especificar");
                if (sinEsp != null) sinEsp.Value += sinEspTotal;
                else mediosPagoLista.Add(new DashboardSerieDto { Label = "Sin Especificar", Value = sinEspTotal });
            }

            mediosPagoLista = mediosPagoLista.OrderByDescending(x => x.Value).ToList();
        }

        // =================================================================
        // 3 & 4. VENTAS POR PROVEEDOR y POR RUBRO  (via Producto)
        // =================================================================
        var productoIds = todasLineas
            .Where(x => x.FkProducto.HasValue)
            .Select(x => x.FkProducto!.Value)
            .Distinct().ToList();

        List<DashboardSerieDto> tortaProveedor = new();
        List<DashboardSerieDto> tortaRubro     = new();

        if (productoIds.Any())
        {
            var productosData = await _db.Productos
                .Where(p => productoIds.Contains(p.Id))
                .Select(p => new { p.Id, p.FkProveedor, p.FkRubro })
                .ToListAsync();

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

            tortaProveedor = todasLineas
                .Where(x => x.FkProducto.HasValue && productoProveedorDict.ContainsKey(x.FkProducto.Value))
                .GroupBy(x => productoProveedorDict[x.FkProducto!.Value])
                .Select(g => new DashboardSerieDto
                {
                    Label = proveedoresDict.TryGetValue(g.Key, out var pn) ? pn : "?",
                    Value = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            tortaRubro = todasLineas
                .Where(x => x.FkProducto.HasValue && productoRubroDict.ContainsKey(x.FkProducto.Value))
                .GroupBy(x => productoRubroDict[x.FkProducto!.Value])
                .Select(g => new DashboardSerieDto
                {
                    Label = rubrosDict.TryGetValue(g.Key, out var rn) ? rn : "Sin Rubro",
                    Value = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Value)
                .ToList();
        }

        // =================================================================
        // 5. TOP 10 CLIENTES DEUDORES (MovimientosCC TipoMovimiento='D')
        // =================================================================
        var movimientos = await _db.MovimientosCC
            .Where(m => m.TipoMovimiento == "D" && m.SaldoPendiente > 0)
            .GroupBy(m => m.ClienteId)
            .Select(g => new { ClienteId = g.Key, Saldo = g.Sum(m => m.SaldoPendiente) })
            .OrderByDescending(x => x.Saldo)
            .Take(10)
            .ToListAsync();

        var deudorIds = movimientos.Select(m => m.ClienteId).ToList();
        var deudoresNombres = deudorIds.Any()
            ? await _db.Clientes
                .Where(c => deudorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.NombreComercial ?? "?")
            : new Dictionary<int, string>();

        var deudores = movimientos
            .Select(m => new DashboardSerieDto
            {
                Label = deudoresNombres.TryGetValue(m.ClienteId, out var nombre) ? nombre : "?",
                Value = m.Saldo
            })
            .ToList();

        return new ClienteGraficosDto
        {
            CurvasLabels   = curvasLabels,
            CurvasSeries   = curvasSeries,
            MediosPago     = mediosPagoLista,
            TortaProveedor = tortaProveedor,
            TortaRubro     = tortaRubro,
            Deudores       = deudores
        };
    }
}
