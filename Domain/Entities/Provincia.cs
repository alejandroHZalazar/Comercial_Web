using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Provincia
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public bool? Baja { get; set; }
}
