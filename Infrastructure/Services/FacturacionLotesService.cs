using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FacturacionLotesService : IFacturacionLotesService
{
    private readonly ComercialDbContext _db;

    public FacturacionLotesService(ComercialDbContext db) => _db = db;

    /// <summary>
    /// Emula sp_Ventas_TraerSinFacturarPorFecha.
    /// Trae ventas del rango de fechas que NO tienen un registro en comprobantes_fiscales
    /// con tipo_comprobante = 'Factura' y nroReferencia = v.id  (equivalente al NOT EXISTS del SP).
    /// </summary>
    public async Task<List<VentaNoFacturadaDto>> GetVentasNoFacturadasAsync(
        DateTime desde, DateTime hasta)
    {
        var hastaFin = hasta.Date.AddDays(1);   // incluye el día completo de "hasta"

        return await (
            from v in _db.Ventas
            join c in _db.Clientes on v.FkCliente equals (int?)c.Id
            join u in _db.Usuarios on v.FkCajero  equals (int?)u.Id
            where v.Fecha >= desde.Date
               && v.Fecha < hastaFin
               && !_db.ComprobantesFiscales.Any(cf =>
                      cf.TipoComprobante == "Factura" &&
                      (long?)cf.NroReferencia == v.Id)
            orderby v.Fecha descending, v.Id descending
            select new VentaNoFacturadaDto
            {
                Nro             = v.Id,
                Fecha           = v.Fecha!.Value,
                TotalVenta      = v.TotalVenta  ?? 0m,
                NombreComercial = c.NombreComercial ?? "",
                Cajero          = u.Nombre      ?? "",
                Iva             = v.Iva         ?? 0m,
                Descuento       = v.Descuento   ?? 0m,
                Recargo         = v.Recargo     ?? 0m,
                Impuesto        = v.Impuesto    ?? 0m
            }
        ).ToListAsync();
    }
}
