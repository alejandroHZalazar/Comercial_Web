namespace Domain.Contracts;

/// <summary>
/// Generación automática de códigos de barras al dar de alta productos (parámetro productos/generaCodigoBarras).
/// </summary>
public interface IGeneradorCodigoBarrasService
{
    /// <summary>Parámetro productos/generaCodigoBarras = '1'.</summary>
    Task<bool> HabilitadoAsync();

    /// <summary>
    /// Reserva <paramref name="cantidad"/> códigos libres (001, 002, …) que no existan como código de barras ni como
    /// código de proveedor en ningún producto, y avanza el contador.
    /// DEBE llamarse dentro de una transacción y ser su primera lectura: bloquea la fila del contador hasta que la
    /// transacción confirme o revierta, de modo que dos altas simultáneas no puedan obtener el mismo código.
    /// </summary>
    /// <param name="excluir">Códigos que ya se van a usar en la misma operación (cargados a mano).</param>
    /// <exception cref="InvalidOperationException">Sin transacción o sin la fila del contador.</exception>
    Task<List<string>> ReservarAsync(int cantidad, IEnumerable<string>? excluir = null);
}
