using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Localidade
{
    public int Id { get; set; }

    public int? FkProvincia { get; set; }

    public string? Nombre { get; set; }

    public bool? Baja { get; set; }
}
