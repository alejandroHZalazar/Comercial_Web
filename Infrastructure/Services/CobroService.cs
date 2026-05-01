using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CobroService : ICobroService
{
    private readonly ComercialDbContext _db;

    public CobroService(ComercialDbContext db) => _db = db;

    // ── Parámetro haceCaja ──────────────────────────────────────────────────
    public async Task<bool> GetHaceCajaAsync()
    {
        var val = await _db.Parametros
            .Where(p => p.Modulo == "caja" && p.Parametro1 == "haceCaja")
            .Select(p => p.Valor)
            .FirstOrDefaultAsync();
        return val == "1";
    }

    // ── Verificar caja abierta (emula sp_Caja_VerificarEstadoUltimaCaja) ───
    public async Task<(bool abierta, int cajaId)> VerificarCajaAsync(int usuarioId)
    {
        var caja = await _db.Cajas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.CajaId)
            .FirstOrDefaultAsync();

        if (caja == null) return (false, 0);
        return (caja.Estado == "ABIERTA", caja.CajaId);
    }

    // ── Obtener userId por nombre de usuario ───────────────────────────────
    public async Task<int> GetUsuarioIdByNombreAsync(string nombreUsuario)
    {
        var id = await _db.Usuarios
            .Where(u => u.Nombre == nombreUsuario)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        return id;
    }

    // ── Planes de pago con datos del medio ─────────────────────────────────
    public async Task<List<PlanPagoConMedioDto>> GetPlanesPagoConMedioAsync()
    {
        return await (
            from p in _db.PlanesPago
            join m in _db.MediosPago on p.FkMedioPago equals m.Id
            orderby m.Nombre, p.Nombre
            select new PlanPagoConMedioDto
            {
                PlanId     = p.Id,
                PlanNombre = p.Nombre,
                MedioId    = m.Id,
                MedioNombre = m.Nombre,
                ConDatos   = m.ConDatos == true,
                Recargo    = p.Recargo ?? 0m
            }
        ).ToListAsync();
    }

    // ── Proceso de cobro principal ─────────────────────────────────────────
    /// <summary>
    /// Emula sp_clientes_Cobrar → sp_cobro_AddCobro → sp_clientes_AddDocumento.
    /// Ejecutado dentro de una transacción EF.
    /// </summary>
    public async Task<int> RealizarCobroAsync(
        int clienteId, decimal importeTotal,
        bool haceCaja, int cajaId,
        List<CobroItemDto> detalle)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // ── 1. Insertar Cobro ─────────────────────────────────────────
            var cobro = new Cobro
            {
                ClienteId    = clienteId,
                Fecha        = DateTime.Now,
                ImporteTotal = importeTotal,
                DocumentoId  = 0,
                TipoCobro    = 1,
                Observaciones = ""
            };
            _db.Cobros.Add(cobro);
            await _db.SaveChangesAsync();

            // ── 2. Insertar líneas + movimientos de caja ──────────────────
            foreach (var item in detalle)
            {
                _db.CobrosDetalle.Add(new CobroDetalle
                {
                    CobroId     = cobro.CobroId,
                    MedioPagoId = item.MedioPagoId,
                    Importe     = item.Importe,
                    Referencia1 = item.Referencia1,
                    Referencia2 = item.Referencia2,
                    Referencia3 = item.Referencia3
                });

                if (haceCaja && cajaId > 0)
                {
                    var conceptoCaja = await _db.ConceptosCaja
                        .FirstOrDefaultAsync(c =>
                            c.FkMedioPago == item.MedioPagoId &&
                            c.Operacion   == "Cobros");

                    if (conceptoCaja != null)
                    {
                        _db.CajaMovimientos.Add(new CajaMovimiento
                        {
                            CajaId         = cajaId,
                            ConceptoCajaId = conceptoCaja.Id,
                            MedioPagoId    = item.MedioPagoId,
                            Importe        = item.Importe,
                            Observaciones  = $"Cobro N° {cobro.CobroId}",
                            Fecha          = DateTime.Now
                        });
                    }
                }
            }
            await _db.SaveChangesAsync();

            // ── 3. Insertar Documento tipo 'RE' ───────────────────────────
            var doc = new Documento
            {
                ClienteId     = clienteId,
                TipoDocumento = "RE",
                Numero        = cobro.CobroId.ToString(),
                Fecha         = DateTime.Now,
                Total         = importeTotal
            };
            _db.Documentos.Add(doc);
            await _db.SaveChangesAsync();

            // ── 4. Insertar MovimientoCC tipo 'C' ─────────────────────────
            var movCC = new MovimientoCC
            {
                ClienteId      = clienteId,
                DocumentoId    = doc.ID,
                Fecha          = DateTime.Now,
                TipoMovimiento = "C",
                Importe        = importeTotal,
                SaldoPendiente = importeTotal
            };
            _db.MovimientosCC.Add(movCC);
            await _db.SaveChangesAsync();

            // ── 5. Imputaciones: reducir débitos pendientes ───────────────
            decimal saldoRestante = importeTotal;

            var pendientes = await _db.MovimientosCC
                .Where(m => m.ClienteId     == clienteId &&
                            m.TipoMovimiento != "C" &&
                            m.SaldoPendiente > 0)
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.MovimientoId)
                .ToListAsync();

            foreach (var pend in pendientes)
            {
                if (saldoRestante <= 0m) break;

                var aImputar = Math.Min(saldoRestante, pend.SaldoPendiente);

                pend.SaldoPendiente  -= aImputar;
                movCC.SaldoPendiente -= aImputar;
                saldoRestante        -= aImputar;

                _db.Imputaciones.Add(new Imputacion
                {
                    MovimientoDebitoId  = pend.MovimientoId,
                    MovimientoCreditoId = movCC.MovimientoId,
                    Importe             = aImputar,
                    Fecha               = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return cobro.CobroId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Datos del cobro para el recibo ─────────────────────────────────────
    public async Task<CobroReciboDto?> GetCobroParaReciboAsync(int cobroId)
    {
        var cobro = await _db.Cobros.FindAsync(cobroId);
        if (cobro == null) return null;

        var cliente = await (
            from c in _db.Clientes
            join l in _db.Localidades on c.FkLocalidad equals l.Id into lj
            from l in lj.DefaultIfEmpty()
            join p in _db.Provincias  on l.FkProvincia  equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where c.Id == cobro.ClienteId
            select new { c, LocalidadNombre = l != null ? l.Nombre : null, ProvinciaNombre = p != null ? p.Nombre : null }
        ).FirstOrDefaultAsync();

        var detalle = await (
            from cd in _db.CobrosDetalle
            join mp in _db.MediosPago on cd.MedioPagoId equals mp.Id
            where cd.CobroId == cobroId
            select new CobroDetalleReciboItem
            {
                MedioPago  = mp.Nombre,
                Importe    = cd.Importe,
                Referencia1 = cd.Referencia1,
                Referencia2 = cd.Referencia2,
                Referencia3 = cd.Referencia3
            }
        ).ToListAsync();

        // Saldo actual de la CC del cliente (post cobro)
        var totalDebe  = await _db.MovimientosCC
            .Where(m => m.ClienteId == cobro.ClienteId && m.TipoMovimiento == "D")
            .SumAsync(m => (decimal?)m.Importe) ?? 0m;
        var totalHaber = await _db.MovimientosCC
            .Where(m => m.ClienteId == cobro.ClienteId && m.TipoMovimiento == "C")
            .SumAsync(m => (decimal?)m.Importe) ?? 0m;

        return new CobroReciboDto
        {
            CobroId        = cobro.CobroId,
            Fecha          = cobro.Fecha,
            ImporteTotal   = cobro.ImporteTotal,
            NombreComercial      = cliente?.c.NombreComercial,
            RazonSocial          = cliente?.c.RazonSocial,
            Direccion            = cliente?.c.Direccion,
            LocalidadDescripcion = cliente?.LocalidadNombre,
            ProvinciaDescripcion = cliente?.ProvinciaNombre,
            Telefono             = cliente?.c.Telefono,
            Cuil                 = cliente?.c.Cuil,
            SaldoPostCobro = totalDebe - totalHaber,
            Detalle        = detalle
        };
    }
}
