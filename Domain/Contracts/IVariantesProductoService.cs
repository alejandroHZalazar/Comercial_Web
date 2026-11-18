namespace Domain.Contracts;

/// <summary>
/// Soporte mínimo de "variantes de producto" (creadas desde LitoralMarket): una ficha principal es una fila
/// de Productos (stock 0) que agrupa variantes. Todo queda inactivo (y no se consulta ninguna tabla de
/// variantes) salvo que el parámetro ecommerce/HabilitarVariantesProducto valga '1' y exista la tabla.
/// </summary>
public interface IVariantesProductoService
{
    /// <summary>true solo con el parámetro en '1' y la tabla productosPrincipales existente (estado cacheado 60 s).</summary>
    Task<bool> HabilitadoAsync();

    /// <summary>Ids (entre los indicados) que son fichas principales. Vacío si las variantes están apagadas.</summary>
    Task<HashSet<int>> FichasPrincipalesAsync(IEnumerable<int> productoIds);

    /// <summary>
    /// Lanza InvalidOperationException si alguno de los productos es una ficha principal (no se vende ni se
    /// pide: hay que elegir una variante). No hace nada si las variantes están apagadas.
    /// </summary>
    Task ValidarNoSonFichasAsync(IEnumerable<int> productoIds);
}
