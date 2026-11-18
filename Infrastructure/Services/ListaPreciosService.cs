using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ListaPreciosService : IListaPreciosService
{
    private const decimal IvaPct = 0.21m;

    private readonly ComercialDbContext _db;
    private readonly IVariantesProductoService _variantes;

    public ListaPreciosService(ComercialDbContext db, IVariantesProductoService variantes)
    {
        _db = db;
        _variantes = variantes;
    }

    /// <summary>
    /// Busca productos activos filtrando por proveedor(es), rubro(s) y/o texto libre.
    /// Todos los filtros se aplican con lógica AND.
    /// El precio con IVA se calcula con tasa fija del 21 %.
    /// </summary>
    public async Task<List<ListaPrecioItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto)
    {
        var provList = proveedorIds?.ToList();
        var rubList  = rubroIds != null ? await RubroJerarquia.ExpandirConDescendientesAsync(_db, rubroIds) : null;
        var txt      = texto?.Trim();

        // ---- LINQ: LEFT JOIN Productos → PreciosProductos ----
        var query =
            from p in _db.Productos
            join pp in _db.PreciosProductos on p.Id equals pp.FkProducto into ppG
            from pp in ppG.DefaultIfEmpty()
            where p.Baja != true
            select new { p, pp };

        // Con variantes habilitadas se muestran solo las variantes: la ficha principal no tiene stock, precio ni
        // código propios para operar. Con el parámetro apagado la consulta no cambia (ni nombra la tabla).
        if (await _variantes.HabilitadoAsync())
            query = query.Where(x => !_db.ProductosPrincipales.Any(f => f.FkProducto == x.p.Id));

        // Filtro por proveedor(es)
        if (provList != null && provList.Count > 0)
            query = query.Where(x =>
                x.p.FkProveedor.HasValue &&
                provList.Contains(x.p.FkProveedor.Value));

        // Filtro por rubro(s)
        if (rubList != null && rubList.Count > 0)
            query = query.Where(x =>
                x.p.FkRubro.HasValue &&
                rubList.Contains(x.p.FkRubro.Value));

        // Filtro por texto (cód. proveedor, cód. barras o descripción)
        if (!string.IsNullOrWhiteSpace(txt))
        {
            var t = txt.ToUpperInvariant();
            query = query.Where(x =>
                (x.p.CodProveedor != null && x.p.CodProveedor.Contains(t)) ||
                (x.p.CodBarras    != null && x.p.CodBarras.Contains(t))    ||
                (x.p.Descripcion  != null && x.p.Descripcion.Contains(t)));
        }

        // Proyección mínima para no traer columnas innecesarias
        var raw = await query
            .OrderBy(x => x.p.Descripcion)
            .Select(x => new
            {
                x.p.Id,
                x.p.CodProveedor,
                x.p.CodBarras,
                x.p.Descripcion,
                Precio = x.pp != null ? x.pp.Precio : (decimal?)null
            })
            .ToListAsync();

        return raw.Select(x =>
        {
            var sinIva = x.Precio ?? 0m;
            return new ListaPrecioItemDto
            {
                ProductoId   = x.Id,
                CodProveedor = x.CodProveedor ?? "",
                CodBarras    = x.CodBarras    ?? "",
                Descripcion  = x.Descripcion  ?? "",
                PrecioSinIva = sinIva,
                PrecioConIva = Math.Round(sinIva * (1m + IvaPct), 4)
            };
        }).ToList();
    }

    /// <summary>
    /// Stock en existencia: productos activos (Baja != true) con stockProductos.cantidad &gt; 0.
    /// Una sola consulta con proyección mínima; el rubro se resuelve en memoria con su ruta completa.
    /// Los totales se calculan con decimal; cada total por producto se redondea a 2 decimales para que
    /// la suma de lo mostrado coincida con los totales generales.
    /// </summary>
    public async Task<List<StockExistenciaDto>> StockExistenciaAsync()
    {
        var raw = await (
            from p in _db.Productos
            join s in _db.StockProductos on p.Id equals s.FkProducto
            join c in _db.CostosProductos on p.Id equals c.FkProducto into cG
            from c in cG.DefaultIfEmpty()
            join pl in _db.PreciosProductos on p.Id equals pl.FkProducto into plG
            from pl in plG.DefaultIfEmpty()
            where p.Baja != true && s.Cantidad > 0
            select new
            {
                p.Id,
                p.CodProveedor,
                p.CodBarras,
                p.Descripcion,
                p.FkRubro,
                Stock  = s.Cantidad,
                Costo  = c  != null ? c.Costo   : (decimal?)null,
                Precio = pl != null ? pl.Precio : (decimal?)null
            })
            .AsNoTracking()
            .ToListAsync();

        var rutas = (await RubroJerarquia.CargarAsync(_db)).ToDictionary(n => n.Id, n => n.Ruta);

        return raw
            .Select(x =>
            {
                var stock  = x.Stock  ?? 0m;
                var costo  = x.Costo  ?? 0m;
                var precio = x.Precio ?? 0m;
                var totalCosto = Math.Round(stock * costo,  2);
                var totalVenta = Math.Round(stock * precio, 2);

                return new StockExistenciaDto
                {
                    ProductoId   = x.Id,
                    Codigo       = !string.IsNullOrWhiteSpace(x.CodProveedor) ? x.CodProveedor!.Trim()
                                 : !string.IsNullOrWhiteSpace(x.CodBarras)    ? x.CodBarras!.Trim()
                                 : x.Id.ToString(),
                    Descripcion  = (x.Descripcion ?? string.Empty).Trim(),
                    Rubro        = x.FkRubro.HasValue && rutas.TryGetValue(x.FkRubro.Value, out var ruta) ? ruta : string.Empty,
                    Stock        = stock,
                    Costo        = costo,
                    PrecioLista  = precio,
                    TotalCosto   = totalCosto,
                    TotalVenta   = totalVenta,
                    Rentabilidad = totalVenta - totalCosto
                };
            })
            .OrderBy(r => r.Rubro,      StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Descripcion, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
