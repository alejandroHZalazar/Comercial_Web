using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReporteVentasService : IReporteVentasService
{
    private readonly ComercialDbContext _db;

    public ReporteVentasService(ComercialDbContext db) => _db = db;

    public async Task<List<ReporteVentaItem>> BuscarVentasAsync(
        DateTime? desde, DateTime? hasta, List<int> clientes)
    {
        var ventasQ = _db.Ventas.AsQueryable();

        if (desde.HasValue)
            ventasQ = ventasQ.Where(v => v.Fecha >= desde.Value.Date);
        if (hasta.HasValue)
            ventasQ = ventasQ.Where(v => v.Fecha < hasta.Value.Date.AddDays(1));
        if (clientes.Count > 0)
            ventasQ = ventasQ.Where(v => v.FkCliente != null && clientes.Contains(v.FkCliente.Value));

        var ventas = await (
            from v in ventasQ
            join c in _db.Clientes on v.FkCliente equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            orderby v.Fecha descending, v.Id descending
            select new
            {
                v.Id, v.Fecha, v.FkCliente, v.TotalVenta, v.TotalCosto,
                v.Iva, v.Impuesto, v.Factura,
                NombreCliente = c != null ? c.NombreComercial : null
            }
        ).ToListAsync();

        if (!ventas.Any()) return new();

        var ventaIds = ventas.Select(v => v.Id).ToList();

        // Formas de pago con nombre de medio
        var ventaIdsInt = ventas.Select(v => (int)v.Id).ToList();
        var fps = await (
            from fp in _db.VentasFormasPago
            where ventaIdsInt.Contains(fp.FkVenta)
            join mp in _db.MediosPago on fp.FkMedioPago equals mp.Id into mpj
            from mp in mpj.DefaultIfEmpty()
            orderby fp.FkVenta, fp.Id
            select new
            {
                fp.FkVenta,
                MedioPago = mp != null ? mp.Nombre : "Efectivo",
                fp.Importe
            }
        ).ToListAsync();

        return ventas.Select(v =>
        {
            return new ReporteVentaItem
            {
                VentaId       = v.Id,
                Fecha         = v.Fecha,
                FkCliente     = v.FkCliente,
                NombreCliente = v.NombreCliente,
                Factura       = v.Factura,
                TotalGeneral  = v.TotalVenta ?? 0m,
                TotalCosto    = v.TotalCosto ?? 0m,
                IvaPct        = v.Iva      ?? 0m,
                IibbPct       = v.Impuesto ?? 0m,
                FormasPago    = fps
                    .Where(fp => fp.FkVenta == (int)v.Id)
                    .Select(fp => new ReporteVentaFormaPago
                    {
                        MedioPago = fp.MedioPago,
                        Importe   = fp.Importe
                    })
                    .ToList()
            };
        }).ToList();
    }
}
