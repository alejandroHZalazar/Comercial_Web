using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class TipoValoresPrecio
{
    public int Id { get; set; }

    public string? Descripcion { get; set; }

    public string? Abrev { get; set; }
}
