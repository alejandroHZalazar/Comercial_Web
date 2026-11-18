using Domain.DTO;

namespace Domain.Contracts;

/// <summary>
/// Administración de productos con variantes (medidas y/o colores), con la misma lógica que LitoralMarket.
/// Cada variante es un producto común más (fila de Productos con su propio stock, precio y costo) enlazado a una
/// ficha principal. Todo se hace solo con ecommerce/HabilitarVariantesProducto = '1' y las tablas creadas.
/// </summary>
public interface IVarianteAdminService
{
    /// <summary>Parámetro en '1' y tablas de variantes existentes. Si es false no se consulta nada de variantes.</summary>
    Task<bool> HabilitadoAsync();

    Task<List<AtributoVarianteDto>> ListarMedidasAsync(bool soloActivas = false);
    Task<List<AtributoVarianteDto>> ListarColoresAsync(bool soloActivas = false);
    Task<ResultadoAtributo> GuardarMedidaAsync(int? id, string nombre, int orden, bool activa);
    Task<ResultadoAtributo> GuardarColorAsync(int? id, string nombre, string? codigoHex, int orden, bool activa);

    /// <summary>
    /// Prepara un listado de búsqueda para el ABM: saca las variantes sueltas y las reemplaza por su ficha (con la
    /// cantidad de variantes habilitadas). Con las variantes apagadas devuelve la lista tal cual, sin consultar nada.
    /// Usa un número fijo de consultas, sin importar cuántos productos haya.
    /// </summary>
    Task<ResultadoListadoVariantes> ResolverListadoAsync(IReadOnlyList<Domain.Entities.Producto> productos);

    /// <summary>Estado de variantes de un producto (TieneVariantes = false si es un producto simple).</summary>
    Task<FichaVariantesDto> ObtenerFichaAsync(int productoId);

    /// <summary>Qué pasaría con las variantes existentes si el costo/precio común pasara a los valores indicados.</summary>
    Task<VistaPreviaPreciosDto> VistaPreviaPrecioComunAsync(int productoId, decimal nuevoCosto, decimal nuevoPrecio);

    /// <summary>
    /// Crea o actualiza la ficha principal y sus variantes en UNA transacción: valida, guarda el producto principal,
    /// genera las descripciones, copia a las variantes los atributos comunes, aplica los precios y habilita o
    /// deshabilita combinaciones. Si algo no valida no se escribe nada.
    /// </summary>
    Task<ResultadoVariantes> GuardarFichaAsync(ProductoDetallesDTO principal, GuardarVariantesRequest request);
}
