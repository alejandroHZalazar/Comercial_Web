namespace Domain.DTO;

// ── Lista (index) ────────────────────────────────────────────────────────────
public class PromocionListDto
{
    public int       Id               { get; set; }
    public int       FkProducto       { get; set; }
    public string    NombreProducto   { get; set; } = string.Empty;
    public string?   CodBarras        { get; set; }
    public bool      Activa           { get; set; }
    public DateTime? FechaDesde       { get; set; }
    public DateTime? FechaHasta       { get; set; }
    public int       CantidadSlots    { get; set; }
}

// ── Edición completa ─────────────────────────────────────────────────────────
public class PromocionDetalleDto
{
    public int            Id             { get; set; }
    public int            FkProducto     { get; set; }
    public string?        NombreProducto { get; set; }
    public bool           Activa         { get; set; } = true;
    public DateTime?      FechaDesde { get; set; }
    public DateTime?      FechaHasta { get; set; }
    public List<SlotDto>  Slots      { get; set; } = new();
}

public class SlotDto
{
    public int              Id                { get; set; }   // 0 = nuevo
    public int              Numero            { get; set; }
    public decimal          CantidadRequerida { get; set; }
    public string?          Descripcion       { get; set; }
    public List<SlotProdDto> Productos        { get; set; } = new();
}

public class SlotProdDto
{
    public int     Id           { get; set; }   // id en promociones_slot_productos
    public int     FkProducto   { get; set; }
    public string  Descripcion  { get; set; } = string.Empty;
    public string? CodBarras    { get; set; }
    public string? CodProveedor { get; set; }
}

// ── Respuesta al buscar promo completa (para el modal de ventas) ─────────────
public class PromocionParaVentaDto
{
    public int              PromocionId { get; set; }
    public int              FkProducto  { get; set; }
    public string           Nombre      { get; set; } = string.Empty;
    public List<SlotVentaDto> Slots     { get; set; } = new();
}

public class SlotVentaDto
{
    public int                   SlotId            { get; set; }
    public int                   Numero            { get; set; }
    public decimal               CantidadRequerida { get; set; }
    public string?               Descripcion       { get; set; }
    public List<SlotProdVentaDto> Productos        { get; set; } = new();
}

public class SlotProdVentaDto
{
    public int     FkProducto   { get; set; }
    public string  Descripcion  { get; set; } = string.Empty;
    public string? CodBarras    { get; set; }
    public string? CodProveedor { get; set; }
    public decimal Stock        { get; set; }
}

// ── Componente elegido al vender ─────────────────────────────────────────────
public class PromoComponenteDto
{
    public int     FkSlot     { get; set; }
    public int     FkProducto { get; set; }
    public decimal Cantidad   { get; set; }
}

// ── Item del carrito enviado desde JS ────────────────────────────────────────
public class ItemCarritoDto
{
    public int     FkProducto { get; set; }
    public decimal Cantidad   { get; set; }
}

// ── Resultado del cálculo de promociones al vender ───────────────────────────
public class CalcularPromocionesResultDto
{
    public bool                          HayPromociones { get; set; }
    public List<PromocionFormadaDto>     Formadas       { get; set; } = new();
    public List<PromocionCasiCompletaDto> CasiCompletas  { get; set; } = new();
}

public class PromocionFormadaDto
{
    public int                       PromocionId       { get; set; }
    public int                       FkProductoPromo   { get; set; }
    public string                    NombrePromo       { get; set; } = string.Empty;
    public int                       Cantidad          { get; set; }
    public List<ComponenteUsadoDto>  ComponentesUsados { get; set; } = new();
}

public class ComponenteUsadoDto
{
    public int     FkSlot       { get; set; }
    public int     FkProducto   { get; set; }
    public string  Descripcion  { get; set; } = string.Empty;
    public decimal CantidadUsada { get; set; }
}

public class PromocionCasiCompletaDto
{
    public int      PromocionId       { get; set; }
    public string   NombrePromo       { get; set; } = string.Empty;
    public string   ProductoFaltante  { get; set; } = string.Empty;
    public string?  CodBarrasFaltante { get; set; }
    public decimal  CantidadFaltante  { get; set; }
}
