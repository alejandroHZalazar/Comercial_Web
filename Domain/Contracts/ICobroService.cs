using Domain.DTO;

namespace Domain.Contracts;

public interface ICobroService
{
    /// <summary>Lee parametro caja/haceCaja. Si = 1 el sistema usa caja por turno.</summary>
    Task<bool> GetHaceCajaAsync();

    /// <summary>Verifica si la última caja del usuario está abierta.</summary>
    Task<(bool abierta, int cajaId)> VerificarCajaAsync(int usuarioId);

    /// <summary>Obtiene el ID numérico del usuario a partir de su nombre de usuario.</summary>
    Task<int> GetUsuarioIdByNombreAsync(string nombreUsuario);

    /// <summary>Retorna todos los planes de pago con los datos de su medio de pago.</summary>
    Task<List<PlanPagoConMedioDto>> GetPlanesPagoConMedioAsync();

    /// <summary>
    /// Registra el cobro emulando sp_clientes_Cobrar:
    /// - Inserta en Cobros + CobrosDetalle (+ movimiento de caja si corresponde)
    /// - Inserta Documento tipo 'RE' + MovimientoCC tipo 'C' + Imputaciones
    /// Retorna el CobroId generado, o lanza excepción.
    /// </summary>
    Task<int> RealizarCobroAsync(int clienteId, decimal importeTotal,
                                 bool haceCaja, int cajaId,
                                 List<CobroItemDto> detalle);

    /// <summary>Datos completos del cobro para imprimir el recibo.</summary>
    Task<CobroReciboDto?> GetCobroParaReciboAsync(int cobroId);
}
