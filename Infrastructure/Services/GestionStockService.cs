using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class GestionStockService : IGestionStockService
{
    private readonly ComercialDbContext _db;

    public GestionStockService(ComercialDbContext db) => _db = db;

    // ---------------------------------------------------------------
    // Búsqueda con todos los datos de precios y stock (5-way LEFT JOIN)
    // ---------------------------------------------------------------
    public async Task<List<GestionStockItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto)
    {
        var provList = proveedorIds?.ToList();
        var rubList  = rubroIds?.ToList();
        var txt      = texto?.Trim();

        var query =
            from p  in _db.Productos.Where(p => p.Baja != true)
            join s  in _db.StockProductos     on p.Id equals s.FkProducto  into sG  from s  in sG.DefaultIfEmpty()
            join c  in _db.CostosProductos    on p.Id equals c.FkProducto  into cG  from c  in cG.DefaultIfEmpty()
            join pl in _db.PreciosProductos   on p.Id equals pl.FkProducto into plG from pl in plG.DefaultIfEmpty()
            join pp in _db.PreciosProveedores on p.Id equals pp.FkProducto into ppG from pp in ppG.DefaultIfEmpty()
            select new { p, s, c, pl, pp };

        if (provList != null && provList.Count > 0)
            query = query.Where(x =>
                x.p.FkProveedor.HasValue && provList.Contains(x.p.FkProveedor.Value));

        if (rubList != null && rubList.Count > 0)
            query = query.Where(x =>
                x.p.FkRubro.HasValue && rubList.Contains(x.p.FkRubro.Value));

        if (!string.IsNullOrWhiteSpace(txt))
        {
            var t = txt.ToUpperInvariant();
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
                x.p.CodProveedor,
                x.p.CodBarras,
                x.p.Descripcion,
                Stock          = x.s  != null ? x.s.Cantidad        : (decimal?)null,
                CantidadMinima = x.s  != null ? x.s.CantidadMinima  : (decimal?)null,
                PrecioCosto    = x.c  != null ? x.c.Costo            : (decimal?)null,
                PrecioProveedor= x.pp != null ? x.pp.Precio          : (decimal?)null,
                PrecioLista    = x.pl != null ? x.pl.Precio          : (decimal?)null
            })
            .ToListAsync();

        return raw.Select(x => new GestionStockItemDto
        {
            ProductoId       = x.Id,
            CodProveedor     = x.CodProveedor  ?? "",
            CodBarras        = x.CodBarras     ?? "",
            Descripcion      = x.Descripcion   ?? "",
            Stock            = x.Stock          ?? 0m,
            CantidadMinima   = x.CantidadMinima ?? 0m,
            PrecioCosto      = x.PrecioCosto    ?? 0m,
            PrecioProveedor  = x.PrecioProveedor ?? 0m,
            PrecioLista      = x.PrecioLista    ?? 0m
        }).ToList();
    }

    // ---------------------------------------------------------------
    // Equivalente LINQ del sp_Productos_AjusteStock
    // ---------------------------------------------------------------
    public async Task<AjusteStockResultDto> AjustarStockAsync(int productoId, decimal nuevoStock)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Descripción del producto
            var productoDesc = await _db.Productos
                .Where(p => p.Id == productoId)
                .Select(p => p.Descripcion)
                .FirstOrDefaultAsync();

            if (productoDesc == null)
                return new AjusteStockResultDto
                {
                    Success = false,
                    Mensaje = $"Producto ID {productoId} no encontrado."
                };

            // Stock actual
            var stockReg = await _db.StockProductos
                .FirstOrDefaultAsync(s => s.FkProducto == productoId);

            var stockAnt = stockReg?.Cantidad ?? 0m;
            var dif      = nuevoStock - stockAnt;

            // Tipo de movimiento: AJUSTE ALTA o AJUSTE BAJA
            var tipoDesc  = dif > 0 ? "AJUSTE ALTA" : "AJUSTE BAJA";
            var tipoMovId = await _db.TipoProductosMovimientos
                .Where(t => t.Descripcion == tipoDesc)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Insertar movimiento (equivale al INSERT del SP)
            _db.ProductosMovimientos.Add(new ProductosMovimiento
            {
                FkProducto     = productoId,
                TipoMovimiento = tipoMovId,
                Descripcion    = productoDesc,
                StockAnt       = stockAnt,
                StockAct       = nuevoStock,
                Cantidad       = Math.Abs(dif),
                FechaMov       = DateTime.Now,
                FkColor        = 0
            });

            // Actualizar stock (equivale al UPDATE del SP)
            if (stockReg != null)
            {
                stockReg.Cantidad = nuevoStock;
            }
            else
            {
                _db.StockProductos.Add(new StockProducto
                {
                    FkProducto = productoId,
                    Cantidad   = nuevoStock
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new AjusteStockResultDto
            {
                Success = true,
                Mensaje = $"Stock actualizado: {stockAnt:N4} → {nuevoStock:N4}"
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new AjusteStockResultDto
            {
                Success = false,
                Mensaje = $"Error: {ex.Message}"
            };
        }
    }
}
