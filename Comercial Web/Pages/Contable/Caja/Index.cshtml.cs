using Application.Interfaces;
using Domain.Contracts;
using Domain.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Data;

namespace Comercial_Web.Pages.Contable.Caja;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ICajaService       _cajaService;
    private readonly IParametroService  _parametroService;
    private readonly ICobroService      _cobroService;
    private readonly IProveedorService  _proveedorService;
    private readonly IMedioPagoService  _medioPagoService;
    private readonly ComercialDbContext _db;

    public IndexModel(
        ICajaService       cajaService,
        IParametroService  parametroService,
        ICobroService      cobroService,
        IProveedorService  proveedorService,
        IMedioPagoService  medioPagoService,
        ComercialDbContext db)
    {
        _cajaService      = cajaService;
        _parametroService = parametroService;
        _cobroService     = cobroService;
        _proveedorService = proveedorService;
        _medioPagoService = medioPagoService;
        _db               = db;
    }

    // ── Datos para la vista ─────────────────────────────────────────────────
    public bool                    HaceCaja          { get; private set; }
    public int                     UsuarioId         { get; private set; }
    public string                  NombreUsuario     { get; private set; } = "";
    public CajaEstadoDto           Estado            { get; private set; } = new();
    public List<CajaResumenItemDto> Resumen          { get; private set; } = new();
    public decimal                 SaldoEfectivo     { get; private set; }
    public decimal                 TotalDebe         { get; private set; }
    public decimal                 TotalHaber        { get; private set; }
    public List<SelectListItem>    ListaMediosPago   { get; private set; } = new();
    // Parámetros de caja para el frontend
    public int                     MedioEfectivoId   { get; private set; }
    public int                     GastoConceptoId   { get; private set; }
    public int                     IngresoConceptoId { get; private set; }
    public int                     EgresoConceptoId  { get; private set; }
    public int                     ArqueoIngresoId   { get; private set; }
    public int                     ArqueoEgresoId    { get; private set; }
    public int                     PagoProvConceptoId { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Parámetro caja/haceCaja
        var p = await _parametroService.ObtenerValorAsync("caja", "haceCaja");
        HaceCaja = p == "1";

        if (!HaceCaja) return Page();   // la vista mostrará el aviso

        await CargarEstadoAsync();
        return Page();
    }

    private async Task CargarEstadoAsync()
    {
        NombreUsuario = User.FindFirstValue(ClaimTypes.Name) ?? "";
        UsuarioId     = await _cobroService.GetUsuarioIdByNombreAsync(NombreUsuario);

        Estado = await _cajaService.GetEstadoUltimaCajaAsync(UsuarioId);

        if (Estado.Abierta)
        {
            Resumen       = await _cajaService.GetResumenAsync(0, UsuarioId);
            TotalDebe     = Resumen.Sum(r => r.ImporteDebe  ?? 0m);
            TotalHaber    = Resumen.Sum(r => r.ImporteHaber ?? 0m);
            SaldoEfectivo = await _cajaService.GetSaldoCajaActualAsync(0, UsuarioId);
        }

        var medios = await _medioPagoService.GetAllAsync();
        ListaMediosPago = medios
            .OrderBy(m => m.Nombre)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Nombre })
            .ToList();

        // Parámetros de caja para el frontend
        var pMedio    = await _parametroService.ObtenerValorAsync("caja", "MedioPagoEfectivo");
        var pGasto    = await _parametroService.ObtenerValorAsync("caja", "Gastos");
        var pIngreso  = await _parametroService.ObtenerValorAsync("caja", "IngresoDinero");
        var pEgreso   = await _parametroService.ObtenerValorAsync("caja", "EgresoDinero");
        var pArqIng  = await _parametroService.ObtenerValorAsync("caja", "ArqueoIngreso");
        var pArqEgr  = await _parametroService.ObtenerValorAsync("caja", "ArqueoEgreso");
        int.TryParse(pMedio,   out int mef); MedioEfectivoId   = mef;
        int.TryParse(pGasto,   out int gco); GastoConceptoId   = gco;
        int.TryParse(pIngreso, out int ico); IngresoConceptoId = ico;
        int.TryParse(pEgreso,  out int eco); EgresoConceptoId  = eco;
        var pPagoProv = await _parametroService.ObtenerValorAsync("caja", "ConceptosPagosProveedores");
        int.TryParse(pArqIng,   out int aig); ArqueoIngresoId    = aig;
        int.TryParse(pArqEgr,   out int aeg); ArqueoEgresoId     = aeg;
        int.TryParse(pPagoProv, out int ppv); PagoProvConceptoId = ppv;
    }

    // ── Refresh JSON (estado + grilla + totales) ────────────────────────────
    public async Task<IActionResult> OnGetEstadoJsonAsync()
    {
        var nombre   = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var usuarioId= await _cobroService.GetUsuarioIdByNombreAsync(nombre);
        var estado   = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
        var resumen  = estado.Abierta ? await _cajaService.GetResumenAsync(0, usuarioId) : new();
        decimal saldo= estado.Abierta ? await _cajaService.GetSaldoCajaActualAsync(0, usuarioId) : 0m;

        return new JsonResult(new
        {
            estado,
            resumen,
            saldoEfectivo = saldo,
            totalDebe     = resumen.Sum(r => r.ImporteDebe  ?? 0m),
            totalHaber    = resumen.Sum(r => r.ImporteHaber ?? 0m)
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ── Tipos de gasto (para combo en modal Gastos) ────────────────────────
    public async Task<IActionResult> OnGetTiposGastoAsync()
    {
        var lista = await _cajaService.GetTiposGastoAsync();
        return new JsonResult(lista, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ── Registro de gasto ───────────────────────────────────────────────────
    public async Task<IActionResult> OnPostGastoAsync([FromBody] GastoNuevoDto dto)
    {
        try
        {
            // Cargar parámetros en el POST (no se transportan desde el GET)
            var pMedio = await _parametroService.ObtenerValorAsync("caja", "MedioPagoEfectivo");
            var pConc  = await _parametroService.ObtenerValorAsync("caja", "Gastos");
            int.TryParse(pMedio, out int medioEfectivoId);
            int.TryParse(pConc,  out int gastoConceptoId);

            if (gastoConceptoId <= 0 || medioEfectivoId <= 0)
                return new JsonResult(new { ok = false, msg = "Los parámetros caja/Gastos o caja/MedioPagoEfectivo no están configurados correctamente. Configure estos valores en Configuración → Parámetros." });

            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
            var estado    = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            if (!estado.Abierta)
                return new JsonResult(new { ok = false, msg = "No hay caja abierta." });

            await _cajaService.AddGastoAsync(estado.CajaId, gastoConceptoId, medioEfectivoId,
                                              dto.Importe, dto.Observaciones, dto.TipoGastoId);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── Conceptos por filtro (para combo en modales) ────────────────────────
    public async Task<IActionResult> OnGetConceptosAsync(string? tipo, bool? afectaEfectivo)
    {
        var lista = await _cajaService.GetConceptosAsync(tipo, afectaEfectivo);
        return new JsonResult(lista, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ── Proveedores activos (para Pago a Proveedores) ───────────────────────
    public async Task<IActionResult> OnGetProveedoresAsync()
    {
        var provs = await _proveedorService.GetAllAsync();
        var data = provs.Where(p => p.Baja != true)
            .OrderBy(p => p.NombreComercial)
            .Select(p => new { id = p.Id, nombre = p.NombreComercial ?? "" })
            .ToList();
        return new JsonResult(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // ── Abrir caja ──────────────────────────────────────────────────────────
    // El saldo inicial es el saldo_cierre de la última caja (= comportamiento de sp_Caja_Apertura).
    // El cliente sólo envía observaciones.
    public async Task<IActionResult> OnPostAbrirAsync([FromBody] AbrirCajaDto dto)
    {
        try
        {
            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);

            // Tomar el saldo_cierre de la última caja como saldo inicial de la nueva
            var ultimaCaja = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            var saldoInicial = ultimaCaja.Existe && ultimaCaja.SaldoCierre.HasValue
                               ? ultimaCaja.SaldoCierre.Value
                               : 0m;

            var id = await _cajaService.AbrirCajaAsync(usuarioId, saldoInicial, dto.Observaciones);
            return new JsonResult(new { ok = true, cajaId = id });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── Cerrar caja ─────────────────────────────────────────────────────────
    // El saldo de cierre se toma del sistema (= traerSaldoCaja), no del cliente.
    public async Task<IActionResult> OnPostCerrarAsync([FromBody] CerrarCajaDto dto)
    {
        try
        {
            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
            var estado    = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            if (!estado.Abierta) return new JsonResult(new { ok = false, msg = "No hay caja abierta." });

            // Calcular saldo de cierre en el servidor (= fn_saldo_caja_actual)
            var saldoCierre = await _cajaService.GetSaldoCajaActualAsync(0, usuarioId);

            await _cajaService.CerrarCajaAsync(estado.CajaId, saldoCierre, dto.Observaciones);
            return new JsonResult(new { ok = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── Movimiento genérico (ingreso/egreso/pago a proveedor) ──────────────
    // Para "ingreso" y "egreso" el concepto y el medio de pago se toman de los
    // parámetros del sistema (caja/IngresoConcepto, caja/EgresoConcepto, caja/MedioPagoEfectivo).
    // El cliente nunca decide esos valores para esas operaciones.
    public async Task<IActionResult> OnPostMovimientoAsync([FromBody] CajaMovimientoNuevoDto dto)
    {
        try
        {
            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
            var estado    = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            if (!estado.Abierta) return new JsonResult(new { ok = false, msg = "No hay caja abierta." });

            int conceptoId = dto.ConceptoCajaId;
            int medioId    = dto.MedioPagoId;

            // Para ingreso y egreso: concepto y medio forzados desde parámetros del sistema
            if (dto.Tipo == "ingreso" || dto.Tipo == "egreso")
            {
                var pMedio   = await _parametroService.ObtenerValorAsync("caja", "MedioPagoEfectivo");
                var pConcepto= dto.Tipo == "ingreso"
                    ? await _parametroService.ObtenerValorAsync("caja", "IngresoDinero")
                    : await _parametroService.ObtenerValorAsync("caja", "EgresoDinero");

                int.TryParse(pMedio,    out int mef);
                int.TryParse(pConcepto, out int cid);

                if (mef <= 0)
                    return new JsonResult(new { ok = false, msg = "El parámetro caja/MedioPagoEfectivo no está configurado." });
                if (cid <= 0)
                    return new JsonResult(new { ok = false, msg = $"El parámetro caja/{(dto.Tipo == "ingreso" ? "IngresoDinero" : "EgresoDinero")} no está configurado." });

                medioId    = mef;
                conceptoId = cid;
            }

            // Si viene proveedor, lo concatenamos a las observaciones
            string? obs = dto.Observaciones;
            if (dto.ProveedorId.HasValue && dto.ProveedorId.Value > 0)
            {
                var prov = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId.Value);
                var nomProv = prov?.NombreComercial ?? "";
                obs = string.IsNullOrWhiteSpace(obs)
                    ? $"Proveedor: {nomProv}"
                    : $"Proveedor: {nomProv} | {obs}";
            }

            var id = await _cajaService.AddMovimientoAsync(estado.CajaId, conceptoId, medioId, dto.Importe, obs);
            return new JsonResult(new { ok = true, id });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── Pago a proveedor ────────────────────────────────────────────────────────
    // Emula sp_Proveedores_AddPAgoCaja:
    //   INSERT INTO Pagos + INSERT INTO movimientos_caja (en transacción).
    // Concepto: caja/PagoProveedores. Medio: caja/MedioPagoEfectivo.
    public async Task<IActionResult> OnPostPagoProvAsync([FromBody] PagoProvDto dto)
    {
        try
        {
            var pConcepto = await _parametroService.ObtenerValorAsync("caja", "ConceptosPagosProveedores");
            var pMedio    = await _parametroService.ObtenerValorAsync("caja", "MedioPagoEfectivo");

            if (!int.TryParse(pConcepto, out int conceptoId) || conceptoId <= 0)
                return new JsonResult(new { ok = false, msg = "El parámetro caja/ConceptosPagosProveedores no está configurado." });
            if (!int.TryParse(pMedio, out int medioId) || medioId <= 0)
                return new JsonResult(new { ok = false, msg = "El parámetro caja/MedioPagoEfectivo no está configurado." });

            if (dto.ProveedorId <= 0)
                return new JsonResult(new { ok = false, msg = "Debe seleccionar un proveedor." });
            if (dto.Importe <= 0)
                return new JsonResult(new { ok = false, msg = "El importe debe ser mayor a cero." });

            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
            var estado    = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            if (!estado.Abierta)
                return new JsonResult(new { ok = false, msg = "No hay caja abierta." });

            // Obtener nombre del proveedor para la observación del movimiento
            var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId);
            var nomProv   = proveedor?.NombreComercial ?? $"Proveedor #{dto.ProveedorId}";

            var pagoId = await _cajaService.AddPagoProveedorAsync(
                estado.CajaId, dto.ProveedorId, nomProv,
                dto.Importe, dto.Observaciones, conceptoId, medioId);

            return new JsonResult(new { ok = true, pagoId });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }

    // ── Arqueo: registra la diferencia como movimiento de ingreso o egreso ─────
    // Emula el comportamiento del frmArqueoCaja del WinForms:
    //   si físico > sistema → AddMovimiento con concepto ArqueoIngreso
    //   si físico < sistema → AddMovimiento con concepto ArqueoEgreso
    //   si igual            → sin movimiento
    // Medio de pago: siempre efectivo (caja/MedioPagoEfectivo).
    public async Task<IActionResult> OnPostArqueoAsync([FromBody] ArqueoNuevoDto dto)
    {
        try
        {
            // Parámetros frescos (no dependen del GET)
            var pMedio   = await _parametroService.ObtenerValorAsync("caja", "MedioPagoEfectivo");
            var pIngreso = await _parametroService.ObtenerValorAsync("caja", "ArqueoIngreso");
            var pEgreso  = await _parametroService.ObtenerValorAsync("caja", "ArqueoEgreso");

            if (!int.TryParse(pIngreso, out int arqueoIngresoId) || arqueoIngresoId <= 0)
                return new JsonResult(new { ok = false, msg = "El parámetro caja/ArqueoIngreso no está configurado." });
            if (!int.TryParse(pEgreso, out int arqueoEgresoId) || arqueoEgresoId <= 0)
                return new JsonResult(new { ok = false, msg = "El parámetro caja/ArqueoEgreso no está configurado." });
            if (!int.TryParse(pMedio, out int medioEfectivoId) || medioEfectivoId <= 0)
                return new JsonResult(new { ok = false, msg = "El parámetro caja/MedioPagoEfectivo no está configurado." });

            var nombre    = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var usuarioId = await _cobroService.GetUsuarioIdByNombreAsync(nombre);
            var estado    = await _cajaService.GetEstadoUltimaCajaAsync(usuarioId);
            if (!estado.Abierta)
                return new JsonResult(new { ok = false, msg = "No hay caja abierta." });

            var saldoSistema = await _cajaService.GetSaldoCajaActualAsync(0, usuarioId);
            var diferencia   = dto.SaldoFisico - saldoSistema;

            if (diferencia > 0)
            {
                // Sobrante: ingreso de arqueo
                await _cajaService.AddMovimientoAsync(estado.CajaId, arqueoIngresoId,
                    medioEfectivoId, diferencia, dto.Observaciones);
            }
            else if (diferencia < 0)
            {
                // Faltante: egreso de arqueo
                await _cajaService.AddMovimientoAsync(estado.CajaId, arqueoEgresoId,
                    medioEfectivoId, Math.Abs(diferencia), dto.Observaciones);
            }
            // Si diferencia == 0 no se genera movimiento

            return new JsonResult(new
            {
                ok           = true,
                diferencia,
                saldoSistema,
                saldoFisico  = dto.SaldoFisico
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, msg = ex.Message });
        }
    }
}
