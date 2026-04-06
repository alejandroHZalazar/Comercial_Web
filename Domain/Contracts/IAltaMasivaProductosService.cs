using Domain.DTO;

namespace Domain.Contracts
{
    public interface IAltaMasivaProductosService
    {
        /// <summary>Verifica si ya existe un producto con igual codProveedor para el mismo proveedor (activo).</summary>
        Task<bool> ExisteProductoAsync(string codProveedor, int fkProveedor);

        /// <summary>Inserta el producto y sus tablas relacionadas (equivale a sp_ProductosABM accion=1).</summary>
        Task<AltaMasivaResultDto> CrearProductoAsync(AltaMasivaFilaDto fila);
    }
}
