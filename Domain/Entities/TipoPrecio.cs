using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TipoPrecio
{
    public int Id { get; set; }

    public string? Descripcion { get; set; }

    public int? FkTipoValor { get; set; }

    public decimal? Valor { get; set; }
}
