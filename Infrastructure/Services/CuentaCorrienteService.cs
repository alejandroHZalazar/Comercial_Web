using Domain.Contracts;
using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CuentaCorrienteService : ICuentaCorrienteService
{
    private readonly ComercialDbContext _db;

    public CuentaCorrienteService(ComercialDbContext db) => _db = db;

    public async Task<List<CuentaCorrienteDto>> GetMovimientosAsync(int clienteId)
    {
        var rows = await FetchRows(clienteId);
        return ToDto(rows);
    }

    public async Task<EstadoCCDto> GetEstadoCCAsync(int clienteId)
    {
        var cutoff = DateTime.Now.AddMonths(-6).Date;
        var rows   = await FetchRows(clienteId);

        var anteriores = rows.Where(r => r.Fecha.Date < cutoff).ToList();
        var recientes  = rows.Where(r => r.Fecha.Date >= cutoff).ToList();

        // Saldo acumulado del período anterior
        var debeAnt  = anteriores.Sum(r => r.TipoMov == "D" ? r.Importe : 0m);
        var haberAnt = anteriores.Sum(r => r.TipoMov == "C" ? r.Importe : 0m);
        var saldoAnt = debeAnt - haberAnt;

        var movimientos = ToDto(recientes);
        var totalDebe   = movimientos.Sum(m => m.Debe);
        var totalHaber  = movimientos.Sum(m => m.Haber);

        return new EstadoCCDto
        {
            SaldoAnterior      = saldoAnt,
            TieneSaldoAnterior = anteriores.Count > 0,
            Movimientos        = movimientos,
            TotalDebe          = totalDebe,
            TotalHaber         = totalHaber,
            SaldoFinal         = saldoAnt + totalDebe - totalHaber
        };
    }

    // ── Filas intermedias (tipo nombrado para poder reutilizarlas) ────────────
    private sealed record CCRow(
        int     MovimientoId,
        DateTime Fecha,
        string  TipoDoc,
        string? Numero,
        string  TipoMov,
        decimal Importe,
        decimal SaldoPendiente);

    private async Task<List<CCRow>> FetchRows(int clienteId)
    {
        return await (
            from mc in _db.MovimientosCC
            join d  in _db.Documentos on mc.DocumentoId equals d.ID
            where mc.ClienteId == clienteId
            orderby d.Fecha, mc.MovimientoId
            select new CCRow(
                mc.MovimientoId,
                d.Fecha,
                d.TipoDocumento,
                d.Numero,
                mc.TipoMovimiento,
                mc.Importe,
                mc.SaldoPendiente)
        ).ToListAsync();
    }

    private static List<CuentaCorrienteDto> ToDto(IEnumerable<CCRow> rows)
    {
        return rows.Select(r =>
        {
            int? cobroId = null;
            if (r.TipoDoc == "RE" && int.TryParse(r.Numero, out var n))
                cobroId = n;

            return new CuentaCorrienteDto
            {
                Fecha            = r.Fecha,
                Movimiento       = r.TipoDoc == "FA" ? "Venta"          :
                                   r.TipoDoc == "RE" ? "Cobro"          :
                                   r.TipoDoc == "NC" ? "Nota de Crédito":
                                   r.TipoDoc == "ND" ? "Nota de Débito" : r.TipoDoc,
                NumeroReferencia = r.Numero,
                Debe             = r.TipoMov == "D" ? r.Importe : 0m,
                Haber            = r.TipoMov == "C" ? r.Importe : 0m,
                Saldo            = r.SaldoPendiente,
                EsCobro          = r.TipoDoc == "RE",
                CobroId          = cobroId
            };
        }).ToList();
    }

    public async Task<List<SaldoClienteDto>> GetSaldosDeudoresAsync(
        List<int> provincias,
        List<int> localidades,
        List<int> vendedores,
        List<int> zonas)
    {
        // 1. Traer clientes activos con sus datos de localidad/provincia/zona/vendedor
        var clientes = await (
            from c in _db.Clientes
            where c.Baja != true
            join l  in _db.Localidades   on c.FkLocalidad equals l.Id  into lj
            from l  in lj.DefaultIfEmpty()
            join p  in _db.Provincias    on (int?)l.FkProvincia equals p.Id into pj
            from p  in pj.DefaultIfEmpty()
            join u  in _db.Usuarios      on c.FkVendedor  equals u.Id  into uj
            from u  in uj.DefaultIfEmpty()
            join z  in _db.ClientesZonas on c.FkZona      equals z.Id  into zj
            from z  in zj.DefaultIfEmpty()
            select new
            {
                c.Id,
                c.NombreComercial,
                c.RazonSocial,
                c.Cuil,
                c.Direccion,
                c.Telefono,
                c.Email,
                FkLocalidad  = c.FkLocalidad,
                FkProvincia  = (int?)l.FkProvincia,
                FkVendedor   = c.FkVendedor,
                FkZona       = c.FkZona,
                Localidad    = l != null ? l.Nombre : null,
                Provincia    = p != null ? p.Nombre : null,
                Vendedor     = u != null ? u.Nombre : null,
                Zona         = z != null ? z.Nombre : null
            }
        ).ToListAsync();

        // 2. Aplicar filtros (si la lista está vacía no se filtra esa dimensión)
        if (provincias.Count  > 0) clientes = clientes.Where(c => c.FkProvincia.HasValue  && provincias .Contains(c.FkProvincia.Value )).ToList();
        if (localidades.Count > 0) clientes = clientes.Where(c => c.FkLocalidad.HasValue  && localidades.Contains(c.FkLocalidad.Value )).ToList();
        if (vendedores.Count  > 0) clientes = clientes.Where(c => c.FkVendedor.HasValue   && vendedores .Contains(c.FkVendedor.Value  )).ToList();
        if (zonas.Count       > 0) clientes = clientes.Where(c => c.FkZona.HasValue       && zonas      .Contains(c.FkZona.Value      )).ToList();

        if (clientes.Count == 0) return new List<SaldoClienteDto>();

        var clienteIds = clientes.Select(c => c.Id).ToList();

        // 3. Traer movimientos CC de esos clientes y calcular saldo
        var movimientos = await _db.MovimientosCC
            .Where(m => clienteIds.Contains(m.ClienteId))
            .Select(m => new { m.ClienteId, m.TipoMovimiento, m.Importe })
            .ToListAsync();

        var saldos = movimientos
            .GroupBy(m => m.ClienteId)
            .Select(g => new
            {
                ClienteId  = g.Key,
                TotalDebe  = g.Where(m => m.TipoMovimiento == "D").Sum(m => m.Importe),
                TotalHaber = g.Where(m => m.TipoMovimiento == "C").Sum(m => m.Importe)
            })
            .Where(s => s.TotalDebe - s.TotalHaber > 0)
            .ToDictionary(s => s.ClienteId);

        // 4. Cruzar y armar resultado (solo clientes con saldo deudor)
        return clientes
            .Where(c => saldos.ContainsKey(c.Id))
            .Select(c =>
            {
                var s = saldos[c.Id];
                return new SaldoClienteDto
                {
                    ClienteId            = c.Id,
                    NombreComercial      = c.NombreComercial,
                    RazonSocial          = c.RazonSocial,
                    Cuil                 = c.Cuil,
                    Direccion            = c.Direccion,
                    LocalidadDescripcion = c.Localidad,
                    ProvinciaDescripcion = c.Provincia,
                    ZonaDescripcion      = c.Zona,
                    Vendedor             = c.Vendedor,
                    Telefono             = c.Telefono,
                    Email                = c.Email,
                    TotalDebe            = s.TotalDebe,
                    TotalHaber           = s.TotalHaber,
                    Saldo                = s.TotalDebe - s.TotalHaber
                };
            })
            .OrderByDescending(c => c.Saldo)
            .ToList();
    }
}
