namespace Domain.Entities;

/// <summary>Regla de promoción por cantidad (NXM) o porcentaje (PORC). Tabla promociones_reglas.</summary>
public class PromocionRegla
{
    public int       Id           { get; set; }
    public string    Nombre       { get; set; } = string.Empty;
    public string    Tipo         { get; set; } = string.Empty;   // PORC | NXM
    public decimal?  Porcentaje   { get; set; }
    public int?      Lleva        { get; set; }
    public int?      Paga         { get; set; }
    /// <summary>Solo NxM: acumula las cantidades de las variantes de una misma ficha (columna agrupaVariantes).</summary>
    public bool      AgrupaVariantes { get; set; }
    public DateTime  VigenteDesde { get; set; }
    public DateTime  VigenteHasta { get; set; }
    public bool      Activa       { get; set; } = true;
    public DateTime  FechaAlta    { get; set; }
    public string?   Usuario      { get; set; }
}

/// <summary>Producto asociado a una regla. Tabla promociones_reglas_productos.</summary>
public class PromocionReglaProducto
{
    public int Id         { get; set; }
    public int FkRegla    { get; set; }
    public int FkProducto { get; set; }
}

/// <summary>Promoción aplicada a una línea de venta (auditoría). Tabla ventas_promo_detalle.</summary>
public class VentaPromoDetalle
{
    public int      Id             { get; set; }
    public long     FkVentaDetalle { get; set; }
    public int?     FkRegla        { get; set; }
    public string   Nombre         { get; set; } = string.Empty;
    public string   Tipo           { get; set; } = string.Empty;
    public decimal? Porcentaje     { get; set; }
    public int?     Lleva          { get; set; }
    public int?     Paga           { get; set; }
    public decimal? CantBonificada { get; set; }
    public decimal  Ahorro         { get; set; }
}

/// <summary>Promoción aplicada a una línea de pedido (auditoría). Tabla pedidos_promo_detalle.</summary>
public class PedidoPromoDetalle
{
    public int      Id               { get; set; }
    public long     FkPedidoDetalle  { get; set; }
    public int?     FkRegla          { get; set; }
    public string   Nombre           { get; set; } = string.Empty;
    public string   Tipo             { get; set; } = string.Empty;
    public decimal? Porcentaje       { get; set; }
    public int?     Lleva            { get; set; }
    public int?     Paga             { get; set; }
    public decimal? CantBonificada   { get; set; }
    public decimal  Ahorro           { get; set; }
}
