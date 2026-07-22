using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public partial class Proveedore
{
    public int Id { get; set; }

    public string? NombreComercial { get; set; }

    public string? Cuil { get; set; }

    public string? Direccion { get; set; }

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public string? Celular { get; set; }

    public decimal? Ganancia { get; set; }

    public bool? Baja { get; set; }

    public decimal? Descuento { get; set; }

    // Columna real (tinyint(1) DEFAULT NULL) — se mantiene nullable por compatibilidad con la BD.
    public bool? PreciosPorProducto { get; set; }

    // Envoltorio no-nullable para el binding de checkboxes en Razor (asp-for exige bool no nullable).
    // No se mapea a la BD; lee/escribe la columna real tratando NULL como false.
    [NotMapped]
    public bool PreciosPorProductoFlag
    {
        get => PreciosPorProducto ?? false;
        set => PreciosPorProducto = value;
    }
}
