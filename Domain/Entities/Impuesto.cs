using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Impuesto
{
    public int Id { get; set; }

    public decimal? Valor { get; set; }
}
