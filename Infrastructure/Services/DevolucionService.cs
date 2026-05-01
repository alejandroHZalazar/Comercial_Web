using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DevolucionService : IDevolucionService
{
    private readonly ComercialDbContext _db;

    public DevolucionService(ComercialDbContext db) => _db = db;

    public async Task<long> GrabarDevolucionAsync(DevolucionRequestDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ── 1. Cabecera Devolución ────────────────────────────────────────
            var dev = new Devolucione
            {
                Fecha           = DateTime.Now,
                TotalDevolucion = dto.Total,
                TotalCosto      = 0m,          // se recalcula abajo con costos reales de BD
                Impuesto        = dto.Impuesto, // IIBB % — 0 si no aplica, nunca null
                FkCliente       = dto.FkCliente,
                FkCajero        = dto.FkCajero,
                Iva             = dto.Iva,
                Descuento       = dto.Descuento > 0 ? dto.Descuento : null,
                Recargo         = dto.Recargo  > 0 ? dto.Recargo  : null,
                FkVendedor      = dto.FkVendedor > 0 ? dto.FkVendedor : null,
                Comision        = dto.Comision
            };
            _db.Devoluciones.Add(dev);
            await _db.SaveChangesAsync();
            long devolucionId = dev.Id;

            // ── 2. Cargar datos de productos en bulk ──────────────────────────
            var productoIds = dto.Filas.Select(f => f.FkProducto).ToList();

            var productos = await _db.Productos
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var costos = await _db.CostosProductos
                .Where(c => c.FkProducto != null && productoIds.Contains(c.FkProducto.Value))
                .ToDictionaryAsync(c => c.FkProducto!.Value, c => c.Costo ?? 0m);

            var stocks = await _db.StockProductos
                .Where(s => s.FkProducto != null && productoIds.Contains(s.FkProducto.Value))
                .ToDictionaryAsync(s => s.FkProducto!.Value);

            // Calcular TotalCosto real desde costosProductos (el frontend envía 0)
            decimal totalCostoReal = dto.Filas.Sum(f =>
            {
                decimal cu = costos.TryGetValue(f.FkProducto, out var c) ? c : 0m;
                return Math.Round(f.Cantidad * cu, 4);
            });

            // Actualizar cabecera con los valores calculados en servidor
            dev.TotalCosto = totalCostoReal;
            dev.Impuesto   = dto.Impuesto;   // IIBB %, siempre grabado (0 si no aplica)
            await _db.SaveChangesAsync();

            // ── 3. Detalle + stock + movimientos ─────────────────────────────
            foreach (var fila in dto.Filas)
            {
                productos.TryGetValue(fila.FkProducto, out var prod);
                decimal costoUnit  = costos.TryGetValue(fila.FkProducto, out var cu) ? cu : 0m;
                decimal costoTotal = Math.Round(fila.Cantidad * costoUnit, 4);

                // Split descRec en descuento/recargo (como el SP)
                decimal descDet = fila.DescRec < 0 ? Math.Abs(fila.DescRec) : 0m;
                decimal recDet  = fila.DescRec > 0 ? fila.DescRec            : 0m;

                // Stock anterior
                stocks.TryGetValue(fila.FkProducto, out var stockEnt);
                decimal stockAnt = stockEnt?.Cantidad ?? 0m;
                decimal stockAct = stockAnt + fila.Cantidad;

                // Insertar detalle
                _db.DevolucionesDetalles.Add(new DevolucionesDetalle
                {
                    FkDevolucion   = (int)devolucionId,
                    FkProducto     = fila.FkProducto,
                    CodBarras      = prod?.CodBarras,
                    Descripcion    = prod?.Descripcion,
                    PrecioSinIva   = fila.PrecioSinIva,
                    PrecioConIva   = fila.PrecioConIva,
                    Cantidad       = fila.Cantidad,
                    Costo          = costoTotal,
                    Subtotal       = Math.Round(fila.Cantidad * fila.PrecioConIva, 4),
                    CodProveedor   = prod?.CodProveedor,
                    Descuento      = descDet > 0 ? descDet : null,
                    Recargo        = recDet  > 0 ? recDet  : null,
                    SubtotalSinIva = fila.SubtotalSinIva
                });

                // Actualizar / crear stock
                if (stockEnt != null)
                    stockEnt.Cantidad = stockAct;
                else
                    _db.StockProductos.Add(new StockProducto
                    {
                        FkProducto = fila.FkProducto,
                        Cantidad   = fila.Cantidad
                    });

                // Movimiento de stock (tipo 4 = Devolución)
                _db.ProductosMovimientos.Add(new ProductosMovimiento
                {
                    FkProducto     = fila.FkProducto,
                    TipoMovimiento = 4,
                    Descripcion    = prod?.Descripcion,
                    StockAnt       = stockAnt,
                    StockAct       = stockAct,
                    Costo          = costoTotal,
                    Venta          = Math.Round(fila.Cantidad * fila.PrecioConIva, 4),
                    Cantidad       = fila.Cantidad,
                    FechaMov       = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();

            // ── 4. Cuenta corriente (si LlevaCC = 1) ─────────────────────────
            if (dto.LlevaCC == 1)
            {
                var cobro = new Cobro
                {
                    ClienteId     = dto.FkCliente,
                    Fecha         = DateTime.Now,
                    ImporteTotal  = dto.Total,
                    EstadoId      = 0,
                    DocumentoId   = 0,
                    TipoCobro     = 2,   // 2 = Nota de Crédito
                    Observaciones = $"Devolución N° {devolucionId}"
                };
                _db.Cobros.Add(cobro);
                await _db.SaveChangesAsync();

                _db.Documentos.Add(new Documento
                {
                    ClienteId     = dto.FkCliente,
                    TipoDocumento = "NC",
                    Numero        = cobro.CobroId.ToString(),
                    Fecha         = DateTime.Now,
                    Total         = dto.Total
                });
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return devolucionId;
        }
        catch
        {
            await tx.RollbackAsync();
            return -1L;
        }
    }

    public async Task<DevolucionImpresionDto?> GetDevolucionParaImpresionAsync(long devolucionId)
    {
        var dRow = await (
            from d    in _db.Devoluciones
            where d.Id == (int)devolucionId
            join c    in _db.Clientes    on d.FkCliente   equals (int?)c.Id into cj
            from c    in cj.DefaultIfEmpty()
            join ci   in _db.CondIvas   on c.FkCondIva   equals ci.Id      into cij
            from ci   in cij.DefaultIfEmpty()
            join l    in _db.Localidades on c.FkLocalidad equals l.Id       into lj
            from l    in lj.DefaultIfEmpty()
            join prov in _db.Provincias  on (int?)l.FkProvincia equals prov.Id into pj
            from prov in pj.DefaultIfEmpty()
            select new
            {
                d.Id, d.Fecha, d.TotalDevolucion, d.Iva, d.Descuento, d.Recargo, d.Impuesto,
                NombreCliente      = c    != null ? c.NombreComercial : null,
                RazonSocial        = c    != null ? c.RazonSocial     : null,
                Cuil               = c    != null ? c.Cuil            : null,
                Direccion          = c    != null ? c.Direccion       : null,
                LocalidadNombre    = l    != null ? l.Nombre          : null,
                ProvinciaNombre    = prov != null ? prov.Nombre       : null,
                CondIvaAbrev       = ci   != null ? ci.Abrev          : null,
                CondIvaDescripcion = ci   != null ? ci.Descripcion    : null
            }
        ).FirstOrDefaultAsync();

        if (dRow == null) return null;

        var detalle = await (
            from dd in _db.DevolucionesDetalles
            where dd.FkDevolucion == (int)devolucionId
            select new DevolucionDetalleImpresionItemDto
            {
                FkProducto     = dd.FkProducto     ?? 0,
                Descripcion    = dd.Descripcion,
                CodBarras      = dd.CodBarras,
                CodProveedor   = dd.CodProveedor,
                Cantidad       = dd.Cantidad       ?? 0m,
                PrecioSinIva   = dd.PrecioSinIva   ?? 0m,
                PrecioConIva   = dd.PrecioConIva   ?? 0m,
                Descuento      = dd.Descuento,
                Recargo        = dd.Recargo,
                SubtotalSinIva = dd.SubtotalSinIva ?? 0m
            }
        ).ToListAsync();

        return new DevolucionImpresionDto
        {
            DevolucionId     = dRow.Id,
            Fecha            = dRow.Fecha,
            NombreCliente    = dRow.NombreCliente,
            RazonSocial      = dRow.RazonSocial,
            Cuil             = dRow.Cuil,
            DireccionCliente = string.Join(", ",
                new[] { dRow.Direccion, dRow.LocalidadNombre, dRow.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
            CondIvaAbrev       = dRow.CondIvaAbrev,
            CondIvaDescripcion = dRow.CondIvaDescripcion,
            Iva              = dRow.Iva             ?? 0m,
            Descuento        = dRow.Descuento,
            Recargo          = dRow.Recargo,
            Impuesto         = dRow.Impuesto        ?? 0m,
            TotalDevolucion  = dRow.TotalDevolucion ?? 0m,
            Detalle          = detalle
        };
    }
}
