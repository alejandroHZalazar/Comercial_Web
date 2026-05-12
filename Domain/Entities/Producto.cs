using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Producto
{
    public int Id { get; set; }

    public string? CodProveedor { get; set; }

    public string? CodBarras { get; set; }

    public int? FkRubro { get; set; }

    public int? Iva { get; set; }

    public string? Descripcion { get; set; }

    public int? FkProveedor { get; set; }

    public bool? Baja { get; set; }

    public bool? Fraccionado { get; set; }

    public bool? Dolarizado { get; set; }

    public bool? EsPromocion { get; set; }

    public string? DescripcionLarga { get; set; }

    public string? Imagen { get; set; }
}
