using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Permiso
{
    public int FkTipoUsuario { get; set; }

    public string? Contenedor { get; set; }

    public string? Control { get; set; }

    public bool? Permiso1 { get; set; }
}
