using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CajaService : ICajaService
{
    private readonly ComercialDbContext _db;
    public CajaService(ComercialDbContext db) => _db = db;

    // ── sp_Caja_VerificarEstadoUltimaCaja ───────────────────────────────────
    public async Task<CajaEstadoDto> GetEstadoUltimaCajaAsync(int usuarioId)
    {
        var ultima = await _db.Cajas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CajaId)
            .FirstOrDefaultAsync();

        if (ultima == null)
            return new CajaEstadoDto { Existe = false, Abierta = false, Estado = "Cerrada" };

        return new CajaEstadoDto
        {
            Existe        = true,
            Abierta       = ultima.Estado == "ABIERTA",
            CajaId        = ultima.CajaId,
            Estado        = ultima.Estado ?? "CERRADA",
            FechaApertura = ultima.FechaApertura,
            FechaCierre   = ultima.FechaCierre,
            SaldoInicial  = ultima.SaldoInicial,
            SaldoCierre   = ultima.SaldoCierre,
            Observaciones = ultima.Observaciones
        };
    }

    // ── sp_caja_ResumenCaja ─────────────────────────────────────────────────
    // Devuelve dos bloques (Debe = egresos, Haber = ingresos), agrupados por concepto.
    public async Task<List<CajaResumenItemDto>> GetResumenAsync(int cajaId, int usuarioId)
    {
        // Filtro de cajas que aplican (la abierta del usuario si cajaId=0, o la puntual)
        var cajaIds = await _db.Cajas
            .Where(c => (cajaId == 0 && c.Estado == "ABIERTA" && c.UsuarioId == usuarioId)
                     || (cajaId  > 0 && c.CajaId  == cajaId))
            .Select(c => c.CajaId)
            .ToListAsync();

        if (cajaIds.Count == 0) return new List<CajaResumenItemDto>();

        // DEBE (egresos)
        var debe = await (
            from mc in _db.CajaMovimientos
            join cc in _db.ConceptosCaja on mc.ConceptoCajaId equals cc.Id
            where cajaIds.Contains(mc.CajaId) && cc.TipoMovimiento == "E"
            group mc by cc.Nombre into g
            select new CajaResumenItemDto
            {
                Debe         = g.Key,
                ImporteDebe  = g.Sum(x => x.Importe),
                Haber        = null,
                ImporteHaber = null,
                Orden        = 1
            }
        ).ToListAsync();

        // HABER (ingresos)
        var haber = await (
            from mc in _db.CajaMovimientos
            join cc in _db.ConceptosCaja on mc.ConceptoCajaId equals cc.Id
            where cajaIds.Contains(mc.CajaId) && cc.TipoMovimiento == "I"
            group mc by cc.Nombre into g
            select new CajaResumenItemDto
            {
                Debe         = null,
                ImporteDebe  = null,
                Haber        = g.Key,
                ImporteHaber = g.Sum(x => x.Importe),
                Orden        = 2
            }
        ).ToListAsync();

        return debe.OrderBy(x => x.Debe)
                   .Concat(haber.OrderBy(x => x.Haber))
                   .ToList();
    }

    // ── fn_saldo_caja_actual ────────────────────────────────────────────────
    // saldo_inicial + Σ(importe si I) − Σ(importe si E)  filtrando afecta_efectivo=1
    public async Task<decimal> GetSaldoCajaActualAsync(int cajaId, int usuarioId)
    {
        var caja = await _db.Cajas
            .Where(c => (cajaId == 0 && c.Estado == "ABIERTA" && c.UsuarioId == usuarioId)
                     || (cajaId  > 0 && c.CajaId  == cajaId))
            .FirstOrDefaultAsync();

        if (caja == null) return 0m;

        decimal movs = await (
            from mc in _db.CajaMovimientos
            join cc in _db.ConceptosCaja on mc.ConceptoCajaId equals cc.Id
            where mc.CajaId == caja.CajaId && cc.AfectaEfectivo
            select cc.TipoMovimiento == "I" ?  mc.Importe
                 : cc.TipoMovimiento == "E" ? -mc.Importe
                 : 0m
        ).SumAsync();

        return caja.SaldoInicial + movs;
    }

    // ── Abrir caja ──────────────────────────────────────────────────────────
    // Emula sp_Caja_Apertura: INSERT INTO caja ... con estado 'ABIERTA'.
    // Las observaciones se prefiján con "Apertura Caja:" (igual que la app WinForms).
    public async Task<int> AbrirCajaAsync(int usuarioId, decimal saldoInicial, string? observaciones)
    {
        // Verificar que no haya una caja ya abierta de este usuario
        bool hayAbierta = await _db.Cajas.AnyAsync(c => c.UsuarioId == usuarioId && c.Estado == "ABIERTA");
        if (hayAbierta) throw new InvalidOperationException("Ya existe una caja ABIERTA para el usuario.");

        // Prefijo de observación igual al WinForms: "Apertura Caja:\n<texto>"
        var obsCompleta = string.IsNullOrWhiteSpace(observaciones)
            ? "Apertura Caja:"
            : $"Apertura Caja:{Environment.NewLine}{observaciones.Trim()}";

        var nueva = new Caja
        {
            UsuarioId     = usuarioId,
            FechaApertura = DateTime.Now,
            FechaCierre   = null,
            SaldoInicial  = saldoInicial,
            SaldoCierre   = null,
            Estado        = "ABIERTA",
            Observaciones = obsCompleta
        };

        _db.Cajas.Add(nueva);
        await _db.SaveChangesAsync();
        return nueva.CajaId;
    }

    // ── Cerrar caja ─────────────────────────────────────────────────────────
    // Emula sp_Caja_Cierre:
    //   UPDATE caja SET fecha_cierre=NOW(), saldo_cierre=?, estado='CERRADA',
    //                   observaciones = CONCAT_WS('', observaciones, unaObservacion)
    // El prefijo del WinForms era: "\nCierre Caja:\n" + textoUsuario
    public async Task CerrarCajaAsync(int cajaId, decimal saldoCierre, string? observaciones)
    {
        var caja = await _db.Cajas.FirstOrDefaultAsync(c => c.CajaId == cajaId);
        if (caja == null) throw new InvalidOperationException("Caja no encontrada.");
        if (caja.Estado != "ABIERTA") throw new InvalidOperationException("La caja ya estaba CERRADA.");

        // Construir sufijo de cierre igual al WinForms:
        //   observacionFinal = Environment.NewLine + "Cierre Caja:" + Environment.NewLine + observacionUsuario
        var sufijo = string.IsNullOrWhiteSpace(observaciones)
            ? $"{Environment.NewLine}Cierre Caja:"
            : $"{Environment.NewLine}Cierre Caja:{Environment.NewLine}{observaciones.Trim()}";

        // CONCAT_WS('', campo_actual, sufijo) → concatenación directa sin separador
        caja.Observaciones = (caja.Observaciones ?? "") + sufijo;
        caja.Estado        = "CERRADA";
        caja.FechaCierre   = DateTime.Now;
        caja.SaldoCierre   = saldoCierre;

        await _db.SaveChangesAsync();
    }

    // ── Alta de movimiento (ingreso, egreso, gasto, pago a proveedor) ───────
    public async Task<int> AddMovimientoAsync(int cajaId, int conceptoId, int medioPagoId,
                                              decimal importe, string? observaciones)
    {
        if (importe <= 0) throw new InvalidOperationException("El importe debe ser mayor a cero.");

        bool cajaOk = await _db.Cajas.AnyAsync(c => c.CajaId == cajaId && c.Estado == "ABIERTA");
        if (!cajaOk) throw new InvalidOperationException("La caja no está abierta.");

        bool conceptoOk = await _db.ConceptosCaja.AnyAsync(c => c.Id == conceptoId);
        if (!conceptoOk) throw new InvalidOperationException("Concepto inválido.");

        var mov = new CajaMovimiento
        {
            CajaId         = cajaId,
            Fecha          = DateTime.Now,
            ConceptoCajaId = conceptoId,
            MedioPagoId    = medioPagoId,
            Importe        = importe,
            Observaciones  = observaciones
        };
        _db.CajaMovimientos.Add(mov);
        await _db.SaveChangesAsync();
        return mov.MovimientoCajaId;
    }

    // ── Auditoría: encabezados ─────────────────────────────────────────────────
    // Emula sp_caja_TraerEncabezadoPorUsuarioyFecha (adaptado para múltiples usuarios).
    public async Task<List<CajaEncabezadoDto>> GetEncabezadosAsync(
        List<int> usuarioIds, DateTime desde, DateTime hasta)
    {
        var hastaFin = hasta.Date.AddDays(1);

        var query = _db.Cajas
            .Join(_db.Usuarios, c => c.UsuarioId, u => u.Id,
                  (c, u) => new { c, u })
            .Where(x => x.c.FechaApertura >= desde.Date
                     && x.c.FechaApertura <  hastaFin);

        if (usuarioIds.Count > 0)
            query = query.Where(x => usuarioIds.Contains(x.c.UsuarioId));

        return await query
            .OrderByDescending(x => x.c.FechaApertura)
            .Select(x => new CajaEncabezadoDto
            {
                CajaId        = x.c.CajaId,
                Usuario       = x.u.Nombre ?? "",
                FechaApertura = x.c.FechaApertura,
                FechaCierre   = x.c.FechaCierre,
                SaldoApertura = x.c.SaldoInicial,
                SaldoCierre   = x.c.SaldoCierre,
                Estado        = x.c.Estado,
                Observaciones = x.c.Observaciones
            })
            .ToListAsync();
    }

    // ── Auditoría: detalle de movimientos ──────────────────────────────────────
    // Emula sp_caja_detalleMovimiento (adaptado para múltiples usuarios).
    public async Task<List<CajaDetalleMovimientoDto>> GetDetalleMovimientosAsync(
        List<int> usuarioIds, DateTime desde, DateTime hasta)
    {
        var hastaFin = hasta.Date.AddDays(1);

        var query = from mc in _db.CajaMovimientos
                    join cc in _db.ConceptosCaja on mc.ConceptoCajaId equals cc.Id
                    join mp in _db.MediosPago    on mc.MedioPagoId    equals mp.Id
                    join c  in _db.Cajas         on mc.CajaId         equals c.CajaId
                    where mc.Fecha >= desde.Date && mc.Fecha < hastaFin
                    select new { mc, cc, mp, c };

        if (usuarioIds.Count > 0)
            query = query.Where(x => usuarioIds.Contains(x.c.UsuarioId));

        return await query
            .OrderBy(x => x.mc.Fecha)
            .Select(x => new CajaDetalleMovimientoDto
            {
                NroCaja       = x.mc.CajaId,
                Fecha         = x.mc.Fecha,
                Concepto      = x.cc.Nombre,
                Tipo          = x.cc.TipoMovimiento == "I" ? "Ingreso" : "Egreso",
                MedioPago     = x.mp.Nombre,
                Importe       = x.mc.Importe,
                Observaciones = x.mc.Observaciones
            })
            .ToListAsync();
    }

    // ── Pago a proveedor ── emula sp_Proveedores_AddPAgoCaja ───────────────────
    // Transacción explícita: INSERT INTO Pagos + INSERT INTO movimientos_caja.
    // La observación del movimiento = "obs. Pago a Proveedor: nombreProveedor"
    // (igual que la función CONCAT del SP original).
    public async Task<int> AddPagoProveedorAsync(int cajaId, int proveedorId, string nombreProveedor,
                                                  decimal importe, string? observaciones,
                                                  int conceptoId, int medioPagoId)
    {
        if (importe <= 0)       throw new InvalidOperationException("El importe debe ser mayor a cero.");
        if (proveedorId <= 0)   throw new InvalidOperationException("Debe seleccionar un proveedor.");

        bool cajaOk = await _db.Cajas.AnyAsync(c => c.CajaId == cajaId && c.Estado == "ABIERTA");
        if (!cajaOk) throw new InvalidOperationException("La caja no está abierta.");

        // Construir observación del movimiento igual al SP:
        // CONCAT(unaObserv, '. Pago a Proveedor: ', nomProveedor)
        var obsMov = string.IsNullOrWhiteSpace(observaciones)
            ? $"Pago a Proveedor: {nombreProveedor}"
            : $"{observaciones.Trim()}. Pago a Proveedor: {nombreProveedor}";

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Paso 1: registrar el pago
            var pago = new Pago
            {
                ProveedorId   = proveedorId,
                Fecha         = DateTime.Now,
                ImporteTotal  = importe,
                Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim()
            };
            _db.Pagos.Add(pago);
            await _db.SaveChangesAsync();

            // Paso 2: registrar el movimiento de caja
            var mov = new CajaMovimiento
            {
                CajaId         = cajaId,
                Fecha          = DateTime.Now,
                ConceptoCajaId = conceptoId,
                MedioPagoId    = medioPagoId,
                Importe        = importe,
                Observaciones  = obsMov
            };
            _db.CajaMovimientos.Add(mov);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return pago.PagoId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Conceptos para combos ──────────────────────────────────────────────
    public async Task<List<ConceptoCajaSimpleDto>> GetConceptosAsync(string? tipoMovimiento, bool? afectaEfectivo)
    {
        var q = _db.ConceptosCaja.AsQueryable();
        if (!string.IsNullOrEmpty(tipoMovimiento))
            q = q.Where(c => c.TipoMovimiento == tipoMovimiento);
        if (afectaEfectivo.HasValue)
            q = q.Where(c => c.AfectaEfectivo == afectaEfectivo.Value);

        return await q.OrderBy(c => c.Nombre)
            .Select(c => new ConceptoCajaSimpleDto
            {
                Id             = c.Id,
                Nombre         = c.Nombre,
                TipoMovimiento = c.TipoMovimiento,
                AfectaEfectivo = c.AfectaEfectivo
            }).ToListAsync();
    }

    // ── Tipos de gasto para el combo del modal ──────────────────────────────
    public async Task<List<TipoGastoDto>> GetTiposGastoAsync()
        => await _db.TiposGasto
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoGastoDto { Id = t.Id, Nombre = t.Nombre })
            .ToListAsync();

    // ── Registro de gasto ── emula sp_caja_AddGasto ────────────────────────
    // 1) INSERT INTO movimientos_caja ...
    // 2) INSERT INTO gastos (id_movimiento_caja, id_tipo_gasto)
    public async Task AddGastoAsync(int cajaId, int conceptoId, int medioPagoId,
                                    decimal importe, string? observaciones, int tipoGastoId)
    {
        if (importe <= 0)  throw new InvalidOperationException("El importe debe ser mayor a cero.");
        if (tipoGastoId <= 0) throw new InvalidOperationException("Debe seleccionar un tipo de gasto.");
        if (string.IsNullOrWhiteSpace(observaciones))
            throw new InvalidOperationException("Debe ingresar una observación.");

        bool cajaOk = await _db.Cajas.AnyAsync(c => c.CajaId == cajaId && c.Estado == "ABIERTA");
        if (!cajaOk) throw new InvalidOperationException("La caja no está abierta.");

        // Paso 1: movimiento de caja
        var mov = new CajaMovimiento
        {
            CajaId         = cajaId,
            Fecha          = DateTime.Now,
            ConceptoCajaId = conceptoId,
            MedioPagoId    = medioPagoId,
            Importe        = importe,
            Observaciones  = observaciones.Trim()
        };
        _db.CajaMovimientos.Add(mov);
        await _db.SaveChangesAsync();   // necesario para obtener el ID generado

        // Paso 2: registro en gastos con el ID del movimiento
        var gasto = new Gasto
        {
            IdMovimientoCaja = mov.MovimientoCajaId,
            IdTipoGasto      = tipoGastoId
        };
        _db.Gastos.Add(gasto);
        await _db.SaveChangesAsync();
    }
}
