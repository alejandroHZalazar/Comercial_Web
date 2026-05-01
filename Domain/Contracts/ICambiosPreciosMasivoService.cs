using Domain.DTO;

namespace Domain.Contracts;

public interface ICambiosPreciosMasivoService
{
    /// <summary>
    /// Emula sp_Productos_CambiarPreciosMasivos para una sola fila.
    /// Actualiza preciosProveedores, preciosProductos, costosProductos y productosLog.
    /// Si no encuentra el producto devuelve EsNuevo=true (sin insertar en ProductosACrear).
    /// </summary>
    Task<CambioPrecioResultDto> CambiarPrecioAsync(FilaCsvPrecioDto fila);

    /// <summary>
    /// Inserta el producto nuevo desde la grilla de "No encontrados",
    /// usando el mismo mecanismo que AltaMasivaProductosService.
    /// </summary>
    Task<InsertarProductoNuevoResult> InsertarProductoNuevoAsync(InsertarProductoNuevoRequest req);
}
