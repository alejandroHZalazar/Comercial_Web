using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class VentasExportarService : IVentasExportarService
{
    private readonly ComercialDbContext _db;

    public VentasExportarService(ComercialDbContext db) => _db = db;

    /// <summary>
    /// Equivalente a sp_Ventas_DetalleCSV.
    /// Devuelve cabecera de venta con pivot de hasta 3 medios de pago.
    /// </summary>
    public async Task<List<VentaResumenExportarDto>> GetResumenAsync(DateTime desde, DateTime hasta)
    {
        var desdeDate = desde.Date;
        var hastaExclusivo = hasta.Date.AddDays(1);

        // Medios de pago por venta (equivalente al CTE mp_ordenados)
        var formasPago = await _db.VentasFormasPago
            .Join(_db.MediosPago,
                fp => fp.FkMedioPago,
                mp => mp.Id,
                (fp, mp) => new { fp.FkVenta, mp.Nombre, fp.Id })
            .ToListAsync();

        // Pivot en memoria: hasta 3 medios por venta, ordenados por Id del registro
        var mediosPivot = formasPago
            .GroupBy(x => x.FkVenta)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Id).Select(x => x.Nombre).ToList());

        // Consulta principal de ventas con detalle para calcular Total
        var ventas = await (
            from v in _db.Ventas
            join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
            join c in _db.Clientes on v.FkCliente equals c.Id
            join cajero in _db.Usuarios on v.FkCajero equals cajero.Id
            join vendedor in _db.Usuarios on v.FkVendedor equals vendedor.Id
            where v.Fecha >= desdeDate && v.Fecha < hastaExclusivo
            select new
            {
                v.Id,
                v.Fecha,
                v.TotalCosto,
                Cliente = c.NombreComercial ?? "",
                Cajero = cajero.Nombre ?? "",
                v.Iva,
                v.Descuento,
                v.Recargo,
                Vendedor = vendedor.Nombre ?? "",
                v.Comision,
                v.Impuesto,
                LineaTotal = vd.SubtotalSinIva != null
                    ? vd.SubtotalSinIva.Value * (vd.Cantidad ?? 0m)
                    : (vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)
                      - ((v.Descuento ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)))
                      + ((v.Recargo ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)))
            }).ToListAsync();

        // Agrupar por cabecera (GROUP BY en el SP)
        var agrupado = ventas
            .GroupBy(x => new { x.Id, x.Fecha, x.TotalCosto, x.Cliente, x.Cajero, x.Iva, x.Descuento, x.Recargo, x.Vendedor, x.Comision, x.Impuesto })
            .Select(g =>
            {
                var mp = mediosPivot.GetValueOrDefault((int)g.Key.Id) ?? new List<string>();
                return new VentaResumenExportarDto
                {
                    Nro = g.Key.Id,
                    Fecha = g.Key.Fecha ?? DateTime.MinValue,
                    Total = Math.Round(g.Sum(x => x.LineaTotal), 2),
                    Costo = Math.Round(g.Key.TotalCosto ?? 0m, 2),
                    Cliente = g.Key.Cliente,
                    Cajero = g.Key.Cajero,
                    Iva = Math.Round(g.Key.Iva ?? 0m, 2),
                    Descuento = Math.Round(g.Key.Descuento ?? 0m, 2),
                    Recargo = Math.Round(g.Key.Recargo ?? 0m, 2),
                    Vendedor = g.Key.Vendedor,
                    Comision = Math.Round(g.Key.Comision ?? 0m, 2),
                    Impuesto = Math.Round(g.Key.Impuesto ?? 0m, 2),
                    MedioPago1 = mp.ElementAtOrDefault(0) ?? "Sin Especificar",
                    MedioPago2 = mp.ElementAtOrDefault(1) ?? "Sin Especificar",
                    MedioPago3 = mp.ElementAtOrDefault(2) ?? "Sin Especificar"
                };
            })
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Nro)
            .ToList();

        return agrupado;
    }

    /// <summary>
    /// Equivalente a sp_Ventas_DetalleProductosCSV.
    /// Devuelve línea de detalle por producto vendido.
    /// </summary>
    public async Task<List<VentaDetalleExportarDto>> GetDetalleAsync(DateTime desde, DateTime hasta)
    {
        var desdeDate = desde.Date;
        var hastaExclusivo = hasta.Date.AddDays(1);

        return await (
            from v in _db.Ventas
            join vd in _db.VentasDetalles on v.Id equals vd.FkVenta
            join c in _db.Clientes on v.FkCliente equals c.Id
            join p in _db.Productos on vd.FkProducto equals p.Id
            join prov in _db.Proveedores on p.FkProveedor equals prov.Id
            where v.Fecha >= desdeDate && v.Fecha < hastaExclusivo
            select new VentaDetalleExportarDto
            {
                Venta = v.Id,
                Fecha = v.Fecha ?? DateTime.MinValue,
                Cliente = c.NombreComercial ?? "",
                Proveedor = prov.NombreComercial ?? "",
                CodProveedor = vd.CodProveedor ?? "",
                Producto = vd.Descripcion ?? "",
                Precio = Math.Round(vd.PrecioSinIva ?? 0m, 2),
                Descuento = Math.Round(
                    vd.Descuento != null ? vd.Descuento.Value : (v.Descuento ?? 0m), 2),
                Recargo = Math.Round(
                    vd.Recargo != null ? vd.Recargo.Value : (v.Recargo ?? 0m), 2),
                PrecioSinIva = Math.Round(
                    vd.SubtotalSinIva != null
                        ? vd.SubtotalSinIva.Value
                        : (vd.PrecioSinIva ?? 0m)
                          - ((v.Descuento ?? 0m) / 100m * (vd.PrecioSinIva ?? 0m))
                          + ((v.Recargo ?? 0m) / 100m * (vd.PrecioSinIva ?? 0m)),
                    2),
                Cantidad = Math.Round(vd.Cantidad ?? 0m, 2),
                Costo = Math.Round(vd.Costo ?? 0m, 2),
                Subtotal = Math.Round(
                    vd.SubtotalSinIva != null
                        ? vd.SubtotalSinIva.Value * (vd.Cantidad ?? 0m)
                        : (vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)
                          - ((v.Descuento ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m)))
                          + ((v.Recargo ?? 0m) / 100m * ((vd.PrecioSinIva ?? 0m) * (vd.Cantidad ?? 0m))),
                    2)
            })
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Venta)
            .ToListAsync();
    }
}
