using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CambiosPreciosService : ICambiosPreciosService
{
    private readonly ComercialDbContext _db;

    public CambiosPreciosService(ComercialDbContext db) => _db = db;

    /// <summary>
    /// Emula sp_ProductosTraerParaCambioPrecio con filtros opcionales.
    /// INNER JOIN igual que el SP: solo productos con las 3 tablas de precios.
    /// </summary>
    public async Task<List<CambiosPreciosItemDto>> BuscarAsync(
        IEnumerable<int>? proveedorIds,
        IEnumerable<int>? rubroIds,
        string?           texto)
    {
        var provList = proveedorIds?.ToList();
        var rubList  = rubroIds?.ToList();

        // LINQ equivalente al SP (INNER JOINs sobre las 3 tablas de precios)
        // LEFT JOIN a Proveedores para obtener ganancia/descuento
        var query =
            from p    in _db.Productos
            where p.Baja != true
            join pp   in _db.PreciosProveedores on p.Id equals pp.FkProducto
            join prp  in _db.PreciosProductos   on p.Id equals prp.FkProducto
            join cp   in _db.CostosProductos    on p.Id equals cp.FkProducto
            join prov in _db.Proveedores        on p.FkProveedor equals (int?)prov.Id into provG
            from prov in provG.DefaultIfEmpty()
            select new { p, pp, prp, cp, prov };

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

        // Filtro por texto libre
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
                CodProveedor = x.p.CodProveedor ?? "",
                CodBarras    = x.p.CodBarras    ?? "",
                Descripcion  = x.p.Descripcion  ?? "",
                PProv        = x.pp.Precio  ?? 0m,
                PSiva        = x.prp.Precio ?? 0m,
                Costo        = x.cp.Costo   ?? 0m,
                FkProveedor  = x.p.FkProveedor ?? 0,
                Ganancia     = x.prov != null ? (x.prov.Ganancia  ?? 0m) : 0m,
                Descuento    = x.prov != null ? (x.prov.Descuento ?? 0m) : 0m
            })
            .ToListAsync();

        return raw.Select(x => new CambiosPreciosItemDto
        {
            ProductoId   = x.Id,
            CodProveedor = x.CodProveedor,
            CodBarras    = x.CodBarras,
            Descripcion  = x.Descripcion,
            PProv        = x.PProv,
            PSiva        = x.PSiva,
            Costo        = x.Costo,
            FkProveedor  = x.FkProveedor,
            Ganancia     = x.Ganancia,
            Descuento    = x.Descuento
        }).ToList();
    }

    // ================================================================
    // Actualiza los tres precios directamente (sin recalcular desde
    // ganancia/descuento). Guarda log + baja=false.
    // Equivale al SP pero recibiendo los 3 precios ya calculados.
    // ================================================================
    public async Task<CambioPrecioResultDto> ActualizarPrecioDirectoAsync(
        ActualizarPrecioDirectoRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Resolver el producto (por CodProveedor + proveedor, o solo CodProveedor,
            // o fallback por CodBarras)
            int? idProvNull = req.IdProveedor;
            var producto = await _db.Productos
                .FirstOrDefaultAsync(p =>
                    p.CodProveedor == req.CodProveedor &&
                    p.FkProveedor  == idProvNull);

            if (producto == null)
                producto = await _db.Productos
                    .FirstOrDefaultAsync(p => p.CodProveedor == req.CodProveedor);

            if (producto == null && !string.IsNullOrEmpty(req.CodBarras))
                producto = await _db.Productos
                    .FirstOrDefaultAsync(p => p.CodBarras == req.CodBarras);

            if (producto == null)
                return new CambioPrecioResultDto
                {
                    Success      = false,
                    Mensaje      = "Producto no encontrado.",
                    CodProveedor = req.CodProveedor
                };

            int prodId = producto.Id;

            // Resguardar precios anteriores (DELETE + INSERT en productosLog)
            var logEx = await _db.ProductosLogs
                .Where(l => l.FkProducto == prodId).ToListAsync();
            _db.ProductosLogs.RemoveRange(logEx);

            var oldPProv = await _db.PreciosProveedores
                .Where(p => p.FkProducto == prodId).Select(p => p.Precio).FirstOrDefaultAsync();
            var oldPSiva = await _db.PreciosProductos
                .Where(p => p.FkProducto == prodId).Select(p => p.Precio).FirstOrDefaultAsync();
            var oldCosto = await _db.CostosProductos
                .Where(p => p.FkProducto == prodId).Select(p => p.Costo).FirstOrDefaultAsync();

            _db.ProductosLogs.Add(new ProductosLog
            {
                FkProducto  = prodId,
                PrecioProv  = oldPProv,
                PrecioLista = oldPSiva,
                PrecioCosto = oldCosto,
                ModifDate   = DateOnly.FromDateTime(DateTime.Now)
            });

            // Actualizar preciosProveedores
            var pprov = await _db.PreciosProveedores.FirstOrDefaultAsync(p => p.FkProducto == prodId);
            if (pprov != null) pprov.Precio = req.PProv;
            else _db.PreciosProveedores.Add(new PreciosProveedore { FkProducto = prodId, Precio = req.PProv });

            // Actualizar preciosProductos (P. S/IVA)
            var psiva = await _db.PreciosProductos.FirstOrDefaultAsync(p => p.FkProducto == prodId);
            if (psiva != null) psiva.Precio = req.PSiva;
            else _db.PreciosProductos.Add(new PreciosProducto { FkProducto = prodId, Precio = req.PSiva });

            // Actualizar costosProductos
            var costo = await _db.CostosProductos.FirstOrDefaultAsync(p => p.FkProducto == prodId);
            if (costo != null) costo.Costo = req.Costo;
            else _db.CostosProductos.Add(new CostosProducto { FkProducto = prodId, Costo = req.Costo });

            // Dar de alta si estaba de baja
            producto.Baja = false;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new CambioPrecioResultDto
            {
                Success      = true,
                Mensaje      = "Precios actualizados.",
                ProductoId   = prodId,
                CodProveedor = producto.CodProveedor ?? req.CodProveedor,
                Descripcion  = producto.Descripcion  ?? ""
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return new CambioPrecioResultDto
            {
                Success      = false,
                Mensaje      = $"Error: {ex.Message}",
                CodProveedor = req.CodProveedor
            };
        }
    }
}
