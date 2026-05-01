using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ImpresionEtiquetasService : IImpresionEtiquetasService
{
    private readonly ComercialDbContext _db;

    public ImpresionEtiquetasService(ComercialDbContext db) => _db = db;

    public async Task<List<ImpresionEtiquetasItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto)
    {
        var provList = proveedorIds?.ToList();
        var rubList  = rubroIds?.ToList();

        var query =
            from p    in _db.Productos
            where p.Baja != true
            join prp  in _db.PreciosProductos on p.Id equals prp.FkProducto into prpG
            from prp  in prpG.DefaultIfEmpty()
            join prov in _db.Proveedores on p.FkProveedor equals (int?)prov.Id into provG
            from prov in provG.DefaultIfEmpty()
            join rub  in _db.Rubros on p.FkRubro equals (int?)rub.Id into rubG
            from rub  in rubG.DefaultIfEmpty()
            select new { p, prp, prov, rub };

        if (provList != null && provList.Count > 0)
            query = query.Where(x =>
                x.p.FkProveedor.HasValue &&
                provList.Contains(x.p.FkProveedor.Value));

        if (rubList != null && rubList.Count > 0)
            query = query.Where(x =>
                x.p.FkRubro.HasValue &&
                rubList.Contains(x.p.FkRubro.Value));

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim().ToUpperInvariant();
            query = query.Where(x =>
                (x.p.CodProveedor != null && x.p.CodProveedor.Contains(t)) ||
                (x.p.CodBarras    != null && x.p.CodBarras.Contains(t))    ||
                (x.p.Descripcion  != null && x.p.Descripcion.Contains(t)));
        }

        var raw = await query
            .OrderBy(x => x.p.Descripcion)
            .Select(x => new
            {
                x.p.Id,
                CodProveedor    = x.p.CodProveedor    ?? "",
                CodBarras       = x.p.CodBarras       ?? "",
                Descripcion     = x.p.Descripcion     ?? "",
                ProveedorNombre = x.prov != null ? (x.prov.NombreComercial ?? "") : "",
                RubroNombre     = x.rub  != null ? (x.rub.Descripcion      ?? "") : "",
                PrecioLista     = x.prp  != null ? (x.prp.Precio           ?? 0m) : 0m
            })
            .ToListAsync();

        return raw.Select(x => new ImpresionEtiquetasItemDto
        {
            ProductoId      = x.Id,
            CodProveedor    = x.CodProveedor,
            CodBarras       = x.CodBarras,
            Descripcion     = x.Descripcion,
            ProveedorNombre = x.ProveedorNombre,
            RubroNombre     = x.RubroNombre,
            PrecioLista     = x.PrecioLista
        }).ToList();
    }
}
