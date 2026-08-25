using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ImagenesProducto
{
    public int Id { get; set; }

    public int FkProducto { get; set; }

    public byte[] Imagen { get; set; } = null!;

    public string? ContentType { get; set; }

    public bool EsPrincipal { get; set; }

    public int Orden { get; set; }

    public bool Baja { get; set; }

    public DateTime FechaAlta { get; set; }
}
