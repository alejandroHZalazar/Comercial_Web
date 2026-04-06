using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class MovimientosProductosService : IMovimientosProductosService
{
    private readonly ComercialDbContext _db;

    public MovimientosProductosService(ComercialDbContext db) => _db = db;

    // ================================================================
    // TAB 1 — Buscar lotes de ingreso
    // Agrupa productosMovimientos por (NroComprobante, DATE(FechaMov))
    // y devuelve una cabecera por lote.
    // ================================================================
    public async Task<List<LoteIngresoDto>> BuscarLotesAsync(
        IEnumerable<int>? proveedorIds,
        string?           texto,
        string?           nroComprobante,
        DateTime?         fechaDesde,
        DateTime?         fechaHasta)
    {
        var provList = proveedorIds?.ToList();
        var txt      = texto?.Trim().ToUpperInvariant();
        var nroTxt   = nroComprobante?.Trim().ToUpperInvariant();

        // Base: movimientos con comprobante no nulo
        var query =
            from m in _db.ProductosMovimientos
            where m.NroComprobante != null && m.NroComprobante != ""
            join p in _db.Productos on m.FkProducto equals p.Id into pG
            from p in pG.DefaultIfEmpty()
            join prov in _db.Proveedores on p.FkProveedor equals prov.Id into provG
            from prov in provG.DefaultIfEmpty()
            select new { m, p, prov };

        // Filtro proveedor
        if (provList != null && provList.Count > 0)
            query = query.Where(x =>
                x.p != null && x.p.FkProveedor.HasValue &&
                provList.Contains(x.p.FkProveedor.Value));

        // Filtro texto (cod proveedor, cod barras, descripcion)
        if (!string.IsNullOrEmpty(txt))
            query = query.Where(x =>
                (x.p.CodProveedor != null && x.p.CodProveedor.Contains(txt)) ||
                (x.p.CodBarras    != null && x.p.CodBarras.Contains(txt))    ||
                (x.m.Descripcion  != null && x.m.Descripcion.Contains(txt)));

        // Filtro nro comprobante
        if (!string.IsNullOrEmpty(nroTxt))
            query = query.Where(x =>
                x.m.NroComprobante != null &&
                x.m.NroComprobante.Contains(nroTxt));

        // Filtro fechas (FechaMov)
        if (fechaDesde.HasValue)
            query = query.Where(x => x.m.FechaMov >= fechaDesde.Value);
        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            query = query.Where(x => x.m.FechaMov < hasta);
        }

        var raw = await query
            .Select(x => new
            {
                x.m.NroComprobante,
                x.m.FechaMov,
                ProvNombre = x.prov != null ? x.prov.NombreComercial : "",
                x.m.Costo
            })
            .ToListAsync();

        // Agrupar en memoria por (NroComprobante, fecha-día de FechaMov)
        var lotes = raw
            .GroupBy(x => new
            {
                x.NroComprobante,
                Dia = x.FechaMov.HasValue ? x.FechaMov.Value.Date : (DateTime?)null
            })
            .Select(g => new LoteIngresoDto
            {
                NroComprobante  = g.Key.NroComprobante ?? "",
                FechaMov        = g.First().FechaMov,
                ProveedorNombre = g.First().ProvNombre ?? "",
                CantProductos   = g.Count(),
                TotalCosto      = g.Sum(x => x.Costo ?? 0m)
            })
            .OrderByDescending(l => l.FechaMov)
            .ThenBy(l => l.NroComprobante)
            .ToList();

        return lotes;
    }

    // ================================================================
    // TAB 1 — Detalle de un lote (todos los ítems con ese comprobante + fecha-día)
    // ================================================================
    public async Task<List<LoteIngresoDetalleDto>> BuscarDetallesLoteAsync(
        string   nroComprobante,
        DateTime fechaMov)
    {
        var diaDesde = fechaMov.Date;
        var diaHasta = diaDesde.AddDays(1);

        var raw = await (
            from m   in _db.ProductosMovimientos
            join p   in _db.Productos                on m.FkProducto    equals p.Id   into pG
            from p   in pG.DefaultIfEmpty()
            join tpm in _db.TipoProductosMovimientos on m.TipoMovimiento equals tpm.Id into tpmG
            from tpm in tpmG.DefaultIfEmpty()
            where m.NroComprobante == nroComprobante
               && m.FechaMov >= diaDesde
               && m.FechaMov <  diaHasta
            orderby m.Id
            select new
            {
                m.Id,
                CodProveedor   = p != null ? p.CodProveedor   : "",
                Descripcion    = m.Descripcion,
                m.FechaEntrega,
                TipoDesc       = tpm != null ? tpm.Descripcion : "",
                StockAnt       = m.StockAnt  ?? 0m,
                StockAct       = m.StockAct  ?? 0m,
                Cantidad       = m.Cantidad  ?? 0m,
                Costo          = m.Costo     ?? 0m,
                m.NroComprobante,
                m.FechaMov
            }).ToListAsync();

        return raw.Select(x => new LoteIngresoDetalleDto
        {
            Id             = x.Id,
            CodProveedor   = x.CodProveedor   ?? "",
            Descripcion    = x.Descripcion     ?? "",
            FechaEntrega   = x.FechaEntrega,
            Tipo           = x.TipoDesc        ?? "",
            StockAnt       = x.StockAnt,
            StockAct       = x.StockAct,
            Cantidad       = x.Cantidad,
            Costo          = x.Costo,
            NroComprobante = x.NroComprobante  ?? "",
            FechaMov       = x.FechaMov
        }).ToList();
    }

    // ================================================================
    // TAB 2 — Buscar productos que tuvieron movimientos en el período
    // ================================================================
    public async Task<List<ProductoConMovimientosDto>> BuscarProductosAsync(
        string?   texto,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        var txt = texto?.Trim().ToUpperInvariant();

        var query =
            from m in _db.ProductosMovimientos
            join p in _db.Productos on m.FkProducto equals p.Id into pG
            from p in pG.DefaultIfEmpty()
            where p != null && p.Baja != true
            select new { m, p };

        if (!string.IsNullOrEmpty(txt))
            query = query.Where(x =>
                (x.p.CodProveedor != null && x.p.CodProveedor.Contains(txt)) ||
                (x.p.CodBarras    != null && x.p.CodBarras.Contains(txt))    ||
                (x.p.Descripcion  != null && x.p.Descripcion.Contains(txt)));

        if (fechaDesde.HasValue)
            query = query.Where(x => x.m.FechaMov >= fechaDesde.Value);
        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            query = query.Where(x => x.m.FechaMov < hasta);
        }

        var raw = await query
            .GroupBy(x => new
            {
                x.p.Id,
                x.p.CodProveedor,
                x.p.CodBarras,
                x.p.Descripcion
            })
            .Select(g => new ProductoConMovimientosDto
            {
                ProductoId   = g.Key.Id,
                CodProveedor = g.Key.CodProveedor ?? "",
                CodBarras    = g.Key.CodBarras    ?? "",
                Descripcion  = g.Key.Descripcion  ?? "",
                CantMov      = g.Count()
            })
            .OrderBy(x => x.Descripcion)
            .ToListAsync();

        return raw;
    }

    // ================================================================
    // TAB 2 — Movimientos de un producto en el período
    // Equivale al SP sp_ProductosTraerMovimientos en LINQ.
    // Regla: si tipoMov == 3, la cantidad se muestra negativa (baja).
    // ================================================================
    public async Task<List<MovimientoItemDto>> BuscarMovimientosProductoAsync(
        int       productoId,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        var query =
            from m   in _db.ProductosMovimientos
            join p   in _db.Productos                on m.FkProducto    equals p.Id   into pG
            from p   in pG.DefaultIfEmpty()
            join tpm in _db.TipoProductosMovimientos on m.TipoMovimiento equals tpm.Id
            where m.FkProducto == productoId
            select new { m, p, tpm };

        if (fechaDesde.HasValue)
            query = query.Where(x => x.m.FechaMov >= fechaDesde.Value);
        if (fechaHasta.HasValue)
        {
            var hasta = fechaHasta.Value.Date.AddDays(1);
            query = query.Where(x => x.m.FechaMov < hasta);
        }

        var raw = await query
            .OrderBy(x => x.m.FechaMov)
            .Select(x => new
            {
                CodProveedor   = x.p != null ? x.p.CodProveedor : "",
                x.m.Descripcion,
                x.m.FechaMov,
                TipoDesc       = x.tpm.Descripcion,
                TipoId         = x.tpm.Id,
                StockAnt       = x.m.StockAnt ?? 0m,
                Cantidad       = x.m.Cantidad ?? 0m,
                StockAct       = x.m.StockAct ?? 0m,
                NroComprobante = x.m.NroComprobante
            })
            .ToListAsync();

        return raw.Select(x => new MovimientoItemDto
        {
            CodProveedor   = x.CodProveedor   ?? "",
            Descripcion    = x.Descripcion     ?? "",
            FechaMov       = x.FechaMov,
            Tipo           = x.TipoDesc        ?? "",
            StockAnt       = x.StockAnt,
            // Si tipo == 3 (AJUSTE BAJA / VENTA) → cantidad negativa (igual que el SP)
            Cantidad       = x.TipoId == 3 ? x.Cantidad * -1 : x.Cantidad,
            StockAct       = x.StockAct,
            NroComprobante = x.NroComprobante ?? ""
        }).ToList();
    }
}
