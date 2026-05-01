using Domain.DTO;

namespace Domain.Contracts;

public interface ICajaService
{
    // Estado de la última caja del usuario (equivalente a sp_Caja_VerificarEstadoUltimaCaja)
    Task<CajaEstadoDto> GetEstadoUltimaCajaAsync(int usuarioId);

    // Resumen de la caja abierta del usuario (cajaId=0) o de una caja puntual
    // Equivalente a sp_caja_ResumenCaja
    Task<List<CajaResumenItemDto>> GetResumenAsync(int cajaId, int usuarioId);

    // Saldo de efectivo en caja — equivalente a fn_saldo_caja_actual(cajaId, usuarioId)
    Task<decimal> GetSaldoCajaActualAsync(int cajaId, int usuarioId);

    // Abrir / Cerrar
    Task<int>  AbrirCajaAsync (int usuarioId, decimal saldoInicial, string? observaciones);
    Task       CerrarCajaAsync(int cajaId, decimal saldoCierre, string? observaciones);

    // Movimiento genérico (ingreso, egreso, gasto, pago a proveedor)
    Task<int> AddMovimientoAsync(int cajaId, int conceptoId, int medioPagoId,
                                 decimal importe, string? observaciones);

    // Listas auxiliares
    Task<List<ConceptoCajaSimpleDto>> GetConceptosAsync(string? tipoMovimiento, bool? afectaEfectivo);
    Task<List<TipoGastoDto>>          GetTiposGastoAsync();

    // Registro de gasto (emula sp_caja_AddGasto)
    Task AddGastoAsync(int cajaId, int conceptoId, int medioPagoId,
                       decimal importe, string? observaciones, int tipoGastoId);

    // Auditoría: encabezados de caja por rango de fechas y usuarios
    Task<List<CajaEncabezadoDto>> GetEncabezadosAsync(
        List<int> usuarioIds, DateTime desde, DateTime hasta);

    // Auditoría: detalle de movimientos por rango de fechas y usuarios
    Task<List<CajaDetalleMovimientoDto>> GetDetalleMovimientosAsync(
        List<int> usuarioIds, DateTime desde, DateTime hasta);

    // Pago a proveedor (emula sp_Proveedores_AddPAgoCaja)
    // Inserta en Pagos + movimientos_caja dentro de una transacción.
    Task<int> AddPagoProveedorAsync(int cajaId, int proveedorId, string nombreProveedor,
                                    decimal importe, string? observaciones,
                                    int conceptoId, int medioPagoId);
}
