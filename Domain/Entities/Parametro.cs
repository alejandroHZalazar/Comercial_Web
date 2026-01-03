using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Parametro
{
    public int Id { get; set; }

    public string? Modulo { get; set; }

    public string? Parametro1 { get; set; }

    public string? Valor { get; set; }

    public byte[]? Imagen { get; set; }
}
