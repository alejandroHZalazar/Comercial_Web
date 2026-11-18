namespace Domain.Entities;

/// <summary>
/// Variante de un producto (tabla productosVariantes): cada una ES una fila de Productos con su propio stock,
/// precio y costo, enlazada a su ficha. Para ventas, pedidos y stock es un producto común.
/// </summary>
public class ProductoVariante
{
    /// <summary>Productos.id de la variante (clave primaria).</summary>
    public int FkProducto  { get; set; }
    /// <summary>productosPrincipales.fk_producto (la ficha).</summary>
    public int FkPrincipal { get; set; }
    /// <summary>variantesMedidas.id; 0 = la ficha no usa medidas.</summary>
    public int FkMedida    { get; set; }
    /// <summary>variantesColores.id; 0 = la ficha no usa colores.</summary>
    public int FkColor     { get; set; }
    /// <summary>false = combinación deshabilitada (además Productos.baja = 1).</summary>
    public bool Activa     { get; set; } = true;
    public int  Orden      { get; set; }
    public DateTime FechaAlta { get; set; } = DateTime.Now;
}
