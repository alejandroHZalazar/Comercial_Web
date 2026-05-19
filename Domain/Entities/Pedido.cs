using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Pedido
{
    public int Id { get; set; }

    public decimal? Total { get; set; }

    public DateTime? Fecha { get; set; }

    public int? FkCliente { get; set; }

    public decimal? Iva { get; set; }

    public decimal? Recargo { get; set; }

    public decimal? Descuento { get; set; }

    public int? FkVendedor { get; set; }

    public string? Observacion { get; set; }

    public bool? Impreso { get; set; }

    public bool? Vendido { get; set; }

    // ── Ecommerce ──────────────────────────────────────────────────────────
    public int?     EsEcommerce          { get; set; }
    public string?  DireccionEntrega     { get; set; }
    public string?  DireccionEntregaTexto { get; set; }
    public decimal? CostoEnvio           { get; set; }
}
