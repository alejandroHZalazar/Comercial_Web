namespace Domain.DTO;

// ═══════════════════════════════════════════════════════════════════════════
// ABM de promociones por cantidad / porcentaje
// ═══════════════════════════════════════════════════════════════════════════

public class PromoReglaListDto
{
    public int       Id             { get; set; }
    public string    Nombre         { get; set; } = string.Empty;
    public string    Tipo           { get; set; } = string.Empty;   // PORC | NXM
    public string    Resumen        { get; set; } = string.Empty;   // "20 % de descuento", "3x2"
    public bool      AgrupaVariantes { get; set; }                  // NxM que acumula variantes de una misma ficha
    public DateTime  Desde          { get; set; }
    public DateTime  Hasta          { get; set; }
    public bool      Activa         { get; set; }
    public bool      Vigente        { get; set; }                   // activa y dentro del período (hora Argentina)
    public int       CantProductos  { get; set; }
}

public class PromoReglaProductoDto
{
    public int      ProductoId   { get; set; }
    public string?  CodProveedor { get; set; }
    public string?  CodBarras    { get; set; }
    public string   Descripcion  { get; set; } = string.Empty;
    public string?  Proveedor    { get; set; }
    public string?  Rubro        { get; set; }
    /// <summary>Precio vigente sin IVA (misma fuente que ventas y pedidos). Nunca se persiste.</summary>
    public decimal  PrecioActual { get; set; }
}

public class PromoReglaDetalleDto
{
    public int       Id          { get; set; }
    public string    Nombre      { get; set; } = string.Empty;
    public string    Tipo        { get; set; } = "PORC";
    public decimal?  Porcentaje  { get; set; }
    public int?      Lleva       { get; set; }
    public int?      Paga        { get; set; }
    /// <summary>Solo NxM: acumular las cantidades de las variantes de una misma ficha para completar el grupo.</summary>
    public bool      AgrupaVariantes { get; set; }
    public DateTime  Desde       { get; set; }
    public DateTime  Hasta       { get; set; }
    public bool      Activa      { get; set; } = true;
    public List<int> ProductoIds { get; set; } = new();
    /// <summary>Solo lectura: productos con su precio actual (para la grilla del ABM).</summary>
    public List<PromoReglaProductoDto> Productos { get; set; } = new();
}

public class PromoBusquedaProductosDto
{
    public int Total { get; set; }
    public List<PromoReglaProductoDto> Items { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════
// Evaluación desde la grilla de ventas / pedidos
// ═══════════════════════════════════════════════════════════════════════════

public class PromoEvalLineaDto
{
    public int     Clave        { get; set; }   // índice de la fila en la grilla del navegador
    public int     FkProducto   { get; set; }
    public decimal Cantidad     { get; set; }
    public decimal PrecioSinIva { get; set; }
}

public class PromoEvalLineaResultadoDto
{
    public int      Clave          { get; set; }
    public int      FkProducto     { get; set; }
    public int      FkRegla        { get; set; }
    public string   Nombre         { get; set; } = string.Empty;
    public string   Tipo           { get; set; } = string.Empty;
    public decimal? Porcentaje     { get; set; }
    public int?     Lleva          { get; set; }
    public int?     Paga           { get; set; }
    public decimal  CantBonificada { get; set; }
    public decimal  DescuentoPct   { get; set; }
    public decimal  Ahorro         { get; set; }
}

/// <summary>Una línea de la notificación "Se aplicaron las siguientes promociones".</summary>
public class PromoResumenDto
{
    public int    FkProducto  { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Texto       { get; set; } = string.Empty;   // "20% de descuento", "promoción 2x1 (1 unidad bonificada)"
}

/// <summary>Aviso informativo: el producto está a una unidad de completar un grupo NxM. No modifica ningún importe.</summary>
public class PromoProximaDto
{
    public int      Clave          { get; set; }   // fila de la grilla a la que se asocia el aviso
    public int      FkProducto     { get; set; }
    public int      FkRegla        { get; set; }
    public string   Nombre         { get; set; } = string.Empty;
    public int      Lleva          { get; set; }
    public int      Paga           { get; set; }
    public decimal  CantidadActual { get; set; }
    public string   Descripcion    { get; set; } = string.Empty;   // producto
    public string   Texto          { get; set; } = string.Empty;   // mensaje comercial completo
    public string   TextoCorto     { get; set; } = string.Empty;   // etiqueta de la fila
}

public class PromoEvalRespuestaDto
{
    public bool Habilitado { get; set; }
    public List<PromoEvalLineaResultadoDto> Lineas   { get; set; } = new();
    public List<PromoResumenDto>            Resumen  { get; set; } = new();
    public List<PromoProximaDto>            Proximas { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════
// Validación y expansión al guardar venta / pedido
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Línea tal como llega del navegador al guardar.</summary>
public class PromoGuardarLinea
{
    public int      Indice       { get; set; }      // posición en el detalle original
    public int      FkProducto   { get; set; }
    public decimal  Cantidad     { get; set; }
    public decimal  PrecioSinIva { get; set; }
    /// <summary>Desc/Rec de la línea como lo calcula la grilla: NEGATIVO = descuento, positivo = recargo.</summary>
    public decimal  DescRec      { get; set; }
    /// <summary>Promoción que el navegador dice haber aplicado (null = ninguna).</summary>
    public int?     FkPromoRegla { get; set; }
    /// <summary>Línea que no participa del motor (viene de un pedido ya pactado).</summary>
    public bool     Excluida     { get; set; }
}

public class PromoAplicadaDto
{
    public int      FkRegla        { get; set; }
    public string   Nombre         { get; set; } = string.Empty;
    public string   Tipo           { get; set; } = string.Empty;
    public decimal? Porcentaje     { get; set; }
    public int?     Lleva          { get; set; }
    public int?     Paga           { get; set; }
    public decimal  CantBonificada { get; set; }
    public decimal  Ahorro         { get; set; }
}

/// <summary>
/// Segmento a persistir. Una línea sin promoción produce 1 segmento sin modificaciones (Promo == null y
/// DescuentoPct == null: se usa la línea original tal cual). NxM produce 2 segmentos (cobradas y bonificadas al 100 %).
/// </summary>
public class PromoGuardarSegmento
{
    public int      Indice        { get; set; }
    public decimal  Cantidad      { get; set; }
    /// <summary>null = no tocar el descuento de la línea original. Valor = % de descuento a persistir.</summary>
    public decimal? DescuentoPct  { get; set; }
    public bool     Bonificada    { get; set; }
    public PromoAplicadaDto? Promo { get; set; }
}
