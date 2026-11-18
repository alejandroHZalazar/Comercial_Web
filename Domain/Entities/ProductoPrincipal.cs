namespace Domain.Entities;

/// <summary>
/// Ficha principal de un producto con variantes (tabla productosPrincipales). FkProducto es el Productos.id de la
/// ficha. Si PreciosIguales, su costo y precio son los valores comunes de todas las variantes. Su stock no se
/// usa (siempre 0): el stock es de cada variante.
/// </summary>
public class ProductoPrincipal
{
    public int      FkProducto     { get; set; }
    public bool     UsaMedidas     { get; set; }
    public bool     UsaColores     { get; set; }
    public bool     PreciosIguales { get; set; } = true;
    public DateTime FechaAlta      { get; set; } = DateTime.Now;
}
