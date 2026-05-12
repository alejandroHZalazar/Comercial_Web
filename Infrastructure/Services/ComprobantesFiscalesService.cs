using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ComprobantesFiscalesService : IComprobantesFiscalesService
{
    private readonly ComercialDbContext _db;

    public ComprobantesFiscalesService(ComercialDbContext db) => _db = db;

    // ── Búsqueda principal ────────────────────────────────────────────────────
    public async Task<List<ComprobanteEmitidoDto>> BuscarAsync(BuscarComprobantesDto filtros)
    {
        var hastaFin = filtros.Hasta.Date.AddDays(1).AddTicks(-1);

        var q = from cf in _db.ComprobantesFiscales
                join c in _db.Clientes on cf.FkCliente equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                where cf.TipoComprobante == "Factura"
                   && cf.FechaEmision >= filtros.Desde.Date
                   && cf.FechaEmision <= hastaFin
                select new { cf, NombreComercial = c != null ? c.NombreComercial : null };

        if (!string.IsNullOrWhiteSpace(filtros.Letra))
            q = q.Where(x => x.cf.Letra == filtros.Letra);

        if (!string.IsNullOrWhiteSpace(filtros.FiltroCliente))
        {
            var f = filtros.FiltroCliente.ToUpper();
            q = q.Where(x =>
                (x.NombreComercial != null && x.NombreComercial.ToUpper().Contains(f)) ||
                (x.cf.RazonSocial  != null && x.cf.RazonSocial.ToUpper().Contains(f))  ||
                (x.cf.Cuit         != null && x.cf.Cuit.Contains(filtros.FiltroCliente)));
        }

        var rows = await q
            .OrderByDescending(x => x.cf.FechaEmision)
            .ToListAsync();

        return rows.Select(x => new ComprobanteEmitidoDto
        {
            Id              = x.cf.Id,
            Letra           = x.cf.Letra,
            PuntoVenta      = x.cf.PuntoVenta,
            Numero          = x.cf.Numero,
            FechaEmision    = x.cf.FechaEmision,
            VentaId         = x.cf.NroReferencia,
            RazonSocial     = x.cf.RazonSocial,
            NombreComercial = x.NombreComercial,
            Cuit            = x.cf.Cuit,
            ImporteTotal    = x.cf.ImporteTotal,
            Cae             = x.cf.Cae,
            FechaVtoCae     = x.cf.FechaVencimientoCae,
            Estado          = x.cf.Estado,
            LinkPdf         = x.cf.LinkPdf
        }).ToList();
    }

    // ── Detalle de un comprobante ─────────────────────────────────────────────
    public async Task<ComprobanteDetalleDto?> GetDetalleAsync(long comprobanteId)
    {
        var cf = await _db.ComprobantesFiscales
            .FirstOrDefaultAsync(x => x.Id == comprobanteId && x.TipoComprobante == "Factura");
        if (cf == null) return null;

        // Cliente
        string? nombreComercial = null;
        if (cf.FkCliente.HasValue)
        {
            var cli = await _db.Clientes
                .Where(c => c.Id == cf.FkCliente.Value)
                .Select(c => c.NombreComercial)
                .FirstOrDefaultAsync();
            nombreComercial = cli;
        }

        var dto = new ComprobanteDetalleDto
        {
            ComprobanteId    = cf.Id,
            Letra            = cf.Letra,
            NumeroFormateado = $"{cf.PuntoVenta:D4}-{cf.Numero:D8}",
            FechaEmision     = cf.FechaEmision,
            RazonSocial      = cf.RazonSocial,
            NombreComercial  = nombreComercial,
            Cuit             = cf.Cuit,
            Cae              = cf.Cae,
            FechaVtoCae      = cf.FechaVencimientoCae,
            LinkPdf          = cf.LinkPdf,
            VentaId          = cf.NroReferencia,
            TotalVenta       = cf.ImporteTotal
        };

        // Datos de la venta asociada (si existe)
        if (cf.NroReferencia.HasValue)
        {
            var ventaId = (long)cf.NroReferencia.Value;

            var venta = await _db.Ventas
                .Where(v => v.Id == ventaId)
                .Select(v => new { v.Iva, v.Descuento, v.Recargo, v.Impuesto, v.TotalVenta })
                .FirstOrDefaultAsync();

            if (venta != null)
            {
                dto.Iva       = venta.Iva       ?? 0m;
                dto.Descuento = venta.Descuento;
                dto.Recargo   = venta.Recargo;
                dto.Impuesto  = venta.Impuesto  ?? 0m;
                dto.TotalVenta = venta.TotalVenta ?? cf.ImporteTotal;
            }

            dto.Detalle = await _db.VentasDetalles
                .Where(vd => vd.FkVenta == ventaId)
                .Select(vd => new DetalleItemFEDto
                {
                    Descripcion    = vd.Descripcion,
                    PrecioSinIva   = vd.PrecioSinIva   ?? 0m,
                    PrecioConIva   = vd.PrecioConIva   ?? 0m,
                    Cantidad       = vd.Cantidad       ?? 0m,
                    SubtotalSinIva = vd.SubtotalSinIva ?? 0m,
                    Descuento      = vd.Descuento,
                    Recargo        = vd.Recargo
                })
                .ToListAsync();

            dto.FormasPago = await (
                from vfp in _db.VentasFormasPago
                join mp  in _db.MediosPago on vfp.FkMedioPago equals mp.Id
                where vfp.FkVenta == (int)ventaId
                select new FormaPagoFEDto
                {
                    MedioPago = mp.Nombre,
                    Importe   = vfp.Importe
                }
            ).ToListAsync();
        }

        return dto;
    }

    // ── Estadísticas del período ──────────────────────────────────────────────
    public async Task<EstadisticasFEDto> GetEstadisticasAsync(DateTime desde, DateTime hasta)
    {
        var hastaFin = hasta.Date.AddDays(1).AddTicks(-1);

        var rows = await _db.ComprobantesFiscales
            .Where(cf => cf.TipoComprobante == "Factura"
                      && cf.FechaEmision >= desde.Date
                      && cf.FechaEmision <= hastaFin)
            .Select(cf => new { cf.Letra, cf.ImporteTotal })
            .ToListAsync();

        var total   = rows.Sum(r => r.ImporteTotal);
        var cant    = rows.Count;
        var rowsA   = rows.Where(r => r.Letra == "A").ToList();
        var rowsB   = rows.Where(r => r.Letra == "B").ToList();
        var rowsC   = rows.Where(r => r.Letra == "C").ToList();

        return new EstadisticasFEDto
        {
            TotalEmitidas   = cant,
            TotalImporte    = total,
            PromedioPorFact = cant > 0 ? Math.Round(total / cant, 2) : 0m,
            CantidadA       = rowsA.Count,
            TotalA          = rowsA.Sum(r => r.ImporteTotal),
            CantidadB       = rowsB.Count,
            TotalB          = rowsB.Sum(r => r.ImporteTotal),
            CantidadC       = rowsC.Count,
            TotalC          = rowsC.Sum(r => r.ImporteTotal)
        };
    }
}
