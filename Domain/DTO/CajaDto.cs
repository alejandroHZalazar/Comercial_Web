namespace Domain.DTO;

// ── Estado actual de la caja del usuario ────────────────────────────────────
public class CajaEstadoDto
{
    public bool       Existe        { get; set; }
    public bool       Abierta       { get; set; }
    public int        CajaId        { get; set; }
    public string     Estado        { get; set; } = "Cerrada";
    public DateTime?  FechaApertura { get; set; }
    public DateTime?  FechaCierre   { get; set; }
    public decimal    SaldoInicial  { get; set; }
    public decimal?   SaldoCierre   { get; set; }
    public string?    Observaciones { get; set; }
}

// ── Fila del resumen (Debe / Haber) ─────────────────────────────────────────
public class CajaResumenItemDto
{
    public string?  Debe         { get; set; }
    public decimal? ImporteDebe  { get; set; }
    public string?  Haber        { get; set; }
    public decimal? ImporteHaber { get; set; }
    public int      Orden        { get; set; }
}

// ── Concepto disponible para selección en modales ───────────────────────────
public class ConceptoCajaSimpleDto
{
    public int    Id              { get; set; }
    public string Nombre          { get; set; } = "";
    public string TipoMovimiento  { get; set; } = ""; // I | E
    public bool   AfectaEfectivo  { get; set; }
}

// ── DTO para registrar un movimiento (usado por handlers AJAX) ──────────────
public class CajaMovimientoNuevoDto
{
    /// <summary>"ingreso" | "egreso" | "pagoProv" — el servidor resuelve concepto/medio para ingreso y egreso.</summary>
    public string   Tipo           { get; set; } = "";
    public int      ConceptoCajaId { get; set; }   // ignorado para ingreso/egreso (se toma de parámetros)
    public int      MedioPagoId    { get; set; }   // ignorado para ingreso/egreso (siempre efectivo)
    public decimal  Importe        { get; set; }
    public string?  Observaciones  { get; set; }
    public int?     ProveedorId    { get; set; }   // sólo en Pago a Proveedores
}

// ── DTO para abrir caja ─────────────────────────────────────────────────────
// El saldo inicial ya NO lo envía el cliente: el servidor lo toma del saldo_cierre anterior.
public class AbrirCajaDto
{
    public string? Observaciones { get; set; }
}

// ── DTO para cerrar caja ────────────────────────────────────────────────────
// El saldo de cierre lo calcula el servidor (fn_saldo_caja_actual). El cliente sólo envía observaciones.
public class CerrarCajaDto
{
    public string? Observaciones { get; set; }
}

// ── DTO de entrada para confirmar arqueo ─────────────────────────────────────
public class ArqueoNuevoDto
{
    public decimal  SaldoFisico   { get; set; }
    public string?  Observaciones { get; set; }
}

// ── Resultado de arqueo ─────────────────────────────────────────────────────
public class ArqueoResultDto
{
    public decimal SaldoSistema { get; set; }
    public decimal SaldoFisico  { get; set; }
    public decimal Diferencia   { get; set; }
}

// ── Tipo de gasto (combo modal gastos) ──────────────────────────────────────
public class TipoGastoDto
{
    public int    Id     { get; set; }
    public string Nombre { get; set; } = "";
}

// ── DTO para registrar un gasto desde el modal ───────────────────────────────
public class GastoNuevoDto
{
    public decimal Importe        { get; set; }
    public int     TipoGastoId    { get; set; }
    public string? Observaciones  { get; set; }
}

// ── Encabezado de caja (sp_caja_TraerEncabezadoPorUsuarioyFecha) ─────────────
public class CajaEncabezadoDto
{
    public int       CajaId        { get; set; }
    public string    Usuario       { get; set; } = "";
    public DateTime  FechaApertura { get; set; }
    public DateTime? FechaCierre   { get; set; }
    public decimal   SaldoApertura { get; set; }
    public decimal?  SaldoCierre   { get; set; }
    public string    Estado        { get; set; } = "";
    public string?   Observaciones { get; set; }
}

// ── Fila de detalle de movimiento (sp_caja_detalleMovimiento) ────────────────
public class CajaDetalleMovimientoDto
{
    public int      NroCaja       { get; set; }
    public DateTime Fecha         { get; set; }
    public string   Concepto      { get; set; } = "";
    public string   Tipo          { get; set; } = "";   // "Ingreso" | "Egreso"
    public string   MedioPago     { get; set; } = "";
    public decimal  Importe       { get; set; }
    public string?  Observaciones { get; set; }
}

// ── DTO para pago a proveedor desde caja ────────────────────────────────────
public class PagoProvDto
{
    public int      ProveedorId   { get; set; }
    public decimal  Importe       { get; set; }
    public string?  Observaciones { get; set; }
}

// ── Parámetros de caja para el frontend ─────────────────────────────────────
public class ParametrosCajaDto
{
    public int  MedioEfectivoId  { get; set; }
    public int  GastoConceptoId  { get; set; }
    public bool Valido           { get; set; }
    public string? Error         { get; set; }
}
