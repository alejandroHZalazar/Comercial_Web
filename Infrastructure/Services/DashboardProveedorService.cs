using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DashboardProveedorService : IDashboardProveedorService
{
    private readonly ComercialDbContext _db;

    public DashboardProveedorService(ComercialDbContext db) => _db = db;

    // =====================================================================
    // MÉTRICAS KPI  (emula sp_Dashboard_ComprasMetricasProveedor)
    // =====================================================================
    public async Task<ProveedorMetricasDto> GetMetricasAsync(
        DateTime   desde,
        DateTime   hasta,
        List<int>? proveedorIds)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        // Base: productosMovimientos tipo=2 (compra) + join Productos
        var query = from pm in _db.ProductosMovimientos
                    where pm.TipoMovimiento == 2
                       && pm.FechaMov >= desdeDate
                       && pm.FechaMov < hastaDate
                    join p in _db.Productos on pm.FkProducto equals p.Id
                    select new
                    {
                        Fecha       = pm.FechaMov,
                        FkProveedor = p.FkProveedor,
                        Total       = (pm.Costo ?? 0m) * (pm.Cantidad ?? 0m)
                    };

        // Filtro de proveedor opcional (ninguno/uno/muchos)
        if (proveedorIds != null && proveedorIds.Any())
            query = query.Where(x => x.FkProveedor.HasValue
                                  && proveedorIds.Contains(x.FkProveedor.Value));

        var data = await query.ToListAsync();

        if (!data.Any())
            return new ProveedorMetricasDto();

        decimal totalCompras    = data.Sum(x => x.Total);
        int     cantidadCompras = data
            .Where(x => x.Fecha.HasValue)
            .Select(x => x.Fecha!.Value.Date)
            .Distinct()
            .Count();

        decimal promedio = cantidadCompras > 0 ? totalCompras / cantidadCompras : 0m;

        var mejorDia = data
            .Where(x => x.Fecha.HasValue)
            .GroupBy(x => x.Fecha!.Value.Date)
            .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();

        return new ProveedorMetricasDto
        {
            TotalCompras    = totalCompras,
            CantidadCompras = cantidadCompras,
            PromedioCompras = promedio,
            MejorDia        = mejorDia != null
                ? $"{mejorDia.Fecha:dd/MM/yyyy} — {mejorDia.Total:N2}"
                : "-"
        };
    }

    // =====================================================================
    // GRÁFICOS  (emula sp_Dashboard_ComprasGraficosProveedor)
    // =====================================================================
    public async Task<ProveedorGraficosDto> GetGraficosAsync(
        DateTime   desde,
        DateTime   hasta,
        List<int>? proveedorIds)
    {
        var desdeDate = desde.Date;
        var hastaDate = hasta.Date.AddDays(1);

        var query = from pm in _db.ProductosMovimientos
                    where pm.TipoMovimiento == 2
                       && pm.FechaMov >= desdeDate
                       && pm.FechaMov < hastaDate
                    join p in _db.Productos on pm.FkProducto equals p.Id
                    select new
                    {
                        Fecha       = pm.FechaMov,
                        FkProveedor = p.FkProveedor,
                        FkRubro     = p.FkRubro,
                        Total       = (pm.Costo ?? 0m) * (pm.Cantidad ?? 0m)
                    };

        if (proveedorIds != null && proveedorIds.Any())
            query = query.Where(x => x.FkProveedor.HasValue
                                  && proveedorIds.Contains(x.FkProveedor.Value));

        var data = await query.ToListAsync();

        if (!data.Any())
            return new ProveedorGraficosDto();

        // Resolver nombres de proveedores y rubros
        var provIds  = data.Where(x => x.FkProveedor.HasValue).Select(x => x.FkProveedor!.Value).Distinct().ToList();
        var rubroIds = data.Where(x => x.FkRubro.HasValue).Select(x => x.FkRubro!.Value).Distinct().ToList();

        var provNombres = provIds.Any()
            ? await _db.Proveedores
                .Where(p => provIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.NombreComercial ?? "?")
            : new Dictionary<int, string>();

        var rubroNombres = rubroIds.Any()
            ? await _db.Rubros
                .Where(r => rubroIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Descripcion ?? "?")
            : new Dictionary<int, string>();

        // ---- 1. CURVAS: agrupado por Fecha + Proveedor ----
        var curvasRaw = data
            .Where(x => x.Fecha.HasValue && x.FkProveedor.HasValue)
            .GroupBy(x => new { Fecha = x.Fecha!.Value.Date, x.FkProveedor })
            .Select(g => new
            {
                Fecha     = g.Key.Fecha,
                Proveedor = provNombres.TryGetValue(g.Key.FkProveedor!.Value, out var n) ? n : "?",
                Total     = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        var allDates      = curvasRaw.Select(x => x.Fecha).Distinct().OrderBy(x => x).ToList();
        var allProveedores = curvasRaw.Select(x => x.Proveedor).Distinct().OrderBy(x => x).ToList();

        var curvasLabels  = allDates.Select(d => d.ToString("dd/MM")).ToList();
        var curvasSeries  = allProveedores.Select(prov => new CurvaSerieDto
        {
            Label = prov,
            Data  = allDates.Select(fecha =>
            {
                var item = curvasRaw.FirstOrDefault(x => x.Fecha == fecha && x.Proveedor == prov);
                return item?.Total ?? 0m;
            }).ToList()
        }).ToList();

        // ---- 2. TORTA PROVEEDOR ----
        var tortaProveedor = data
            .Where(x => x.FkProveedor.HasValue)
            .GroupBy(x => x.FkProveedor!.Value)
            .Select(g => new DashboardSerieDto
            {
                Label = provNombres.TryGetValue(g.Key, out var n) ? n : "?",
                Value = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        // ---- 3. TORTA RUBRO ----
        var tortaRubro = data
            .Where(x => x.FkRubro.HasValue)
            .GroupBy(x => x.FkRubro!.Value)
            .Select(g => new DashboardSerieDto
            {
                Label = rubroNombres.TryGetValue(g.Key, out var r) ? r : "Sin Rubro",
                Value = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        return new ProveedorGraficosDto
        {
            CurvasLabels   = curvasLabels,
            CurvasSeries   = curvasSeries,
            TortaProveedor = tortaProveedor,
            TortaRubro     = tortaRubro
        };
    }
}
