using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class NotaCreditoService : INotaCreditoService
{
    private readonly ComercialDbContext _db;
    private readonly IParametroService  _param;
    private readonly IFacturacionElectronicaService _feService;

    public NotaCreditoService(ComercialDbContext db, IParametroService param,
                              IFacturacionElectronicaService feService)
    {
        _db        = db;
        _param     = param;
        _feService = feService;
    }

    // ─── Datos iniciales para el formulario ───────────────────────────────
    public async Task<NotaCreditoDatosIniciales> GetDatosInicialesAsync()
    {
        var feVal = await _param.ObtenerValorAsync("ventas", "facturaElectronica");
        var fe    = feVal == "1";

        var ivas = await _db.IvaPorcentajes
            .Where(i => i.Valor != null)
            .OrderBy(i => i.Valor)
            .Select(i => new IvaItemDto { Id = i.Id, Valor = i.Valor!.Value })
            .ToListAsync();

        var imps = await _db.Impuestos
            .Where(i => i.Valor != null)
            .OrderBy(i => i.Valor)
            .Select(i => new ImpuestoItemDto { Id = i.Id, Valor = i.Valor!.Value })
            .ToListAsync();

        return new NotaCreditoDatosIniciales { FacturaElectronica = fe, Ivas = ivas, Impuestos = imps };
    }

    // ─── Proceso principal ────────────────────────────────────────────────
    public async Task<(int cobroId, string? error)> ProcesarNCAsync(NotaCreditoRequestDto dto)
    {
        // 1. Verificar si hay FE activa
        var feStr  = await _param.ObtenerValorAsync("ventas", "facturaElectronica");
        var fe     = feStr == "1";
        var pvStr  = await _param.ObtenerValorAsync("PuntoVenta", Environment.MachineName);
        var pv     = string.IsNullOrEmpty(pvStr) ? 0 : int.Parse(pvStr);

        // 2. Si FE activa y hay factura asociada → emitir NC electrónica (rama manual)
        if (fe && dto.FacturaAsociada > 0 && pv > 0)
        {
            var (feOk, feError, _) = await _feService.EmitirNotaCreditoManualAsync(dto, pv);
            if (!feOk)
                return (0, feError ?? "Error al emitir la Nota de Crédito electrónica.");
        }

        // 3. Guardar NC en BD (siempre, sin importar FE)
        var cobroId = await GuardarNCEnDBAsync(dto);
        return (cobroId, null);
    }

    // ─── Guardar NC en BD (emula sp_clientes_ADD_NC) ─────────────────────
    private async Task<int> GuardarNCEnDBAsync(NotaCreditoRequestDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Insertar Cobro (TipoCobro=2 para NC)
            var cobro = new Cobro
            {
                ClienteId    = dto.ClienteId,
                Fecha        = DateTime.Now,
                ImporteTotal = dto.Importe,
                DocumentoId  = 0,
                TipoCobro    = 2,
                Observaciones = dto.Observaciones
            };
            _db.Cobros.Add(cobro);
            await _db.SaveChangesAsync();

            // 2. Insertar Documento tipo 'NC'
            var doc = new Documento
            {
                ClienteId     = dto.ClienteId,
                TipoDocumento = "NC",
                Numero        = cobro.CobroId.ToString(),
                Fecha         = DateTime.Now,
                Total         = dto.Importe
            };
            _db.Documentos.Add(doc);
            await _db.SaveChangesAsync();

            // 3. Insertar MovimientoCC tipo 'C' (crédito)
            var movCC = new MovimientoCC
            {
                ClienteId      = dto.ClienteId,
                DocumentoId    = doc.ID,
                Fecha          = DateTime.Now,
                TipoMovimiento = "C",
                Importe        = dto.Importe,
                SaldoPendiente = dto.Importe
            };
            _db.MovimientosCC.Add(movCC);
            await _db.SaveChangesAsync();

            // 4. Imputar contra débitos pendientes (mismo algoritmo que CobroService)
            decimal saldoRestante = dto.Importe;
            var pendientes = await _db.MovimientosCC
                .Where(m => m.ClienteId      == dto.ClienteId &&
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

    // ─── Nota de Débito (emula sp_clientes_AddND) ─────────────────────────
    public async Task<int> ProcesarNDAsync(int clienteId, decimal importe, string observaciones)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Insertar NotaDebito
            var nd = new NotaDebito
            {
                ClienteId     = clienteId,
                Fecha         = DateTime.Now,
                ImporteTotal  = importe,
                Observaciones = observaciones
            };
            _db.NotasDebito.Add(nd);
            await _db.SaveChangesAsync();

            // 2. Insertar Documento tipo 'ND'
            var doc = new Documento
            {
                ClienteId     = clienteId,
                TipoDocumento = "ND",
                Numero        = nd.Id.ToString(),
                Fecha         = DateTime.Now,
                Total         = importe
            };
            _db.Documentos.Add(doc);
            await _db.SaveChangesAsync();

            // 3. Insertar MovimientoCC tipo 'D' (débito)
            var movCC = new MovimientoCC
            {
                ClienteId      = clienteId,
                DocumentoId    = doc.ID,
                Fecha          = DateTime.Now,
                TipoMovimiento = "D",
                Importe        = importe,
                SaldoPendiente = importe
            };
            _db.MovimientosCC.Add(movCC);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return nd.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
