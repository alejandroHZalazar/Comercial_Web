using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ListaPreciosService : IListaPreciosService
{
    private const decimal IvaPct = 0.21m;

    private readonly ComercialDbContext _db;

    public ListaPreciosService(ComercialDbContext db) => _db = db;

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
        var rubList  = rubroIds?.ToList();
        var txt      = texto?.Trim();

        // ---- LINQ: LEFT JOIN Productos → PreciosProductos ----
        var query =
            from p in _db.Productos
            join pp in _db.PreciosProductos on p.Id equals pp.FkProducto into ppG
            from pp in ppG.DefaultIfEmpty()
            where p.Baja != true
            select new { p, pp };

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
}
