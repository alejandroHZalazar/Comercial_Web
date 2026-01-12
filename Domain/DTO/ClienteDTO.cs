using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class ClienteDTO
    {
        public class ClienteDetalleDTO
        {
            public int Id { get; set; }
            public string? NombreComercial { get; set; }
            public string? RazonSocial { get; set; }
            public string? Cuil { get; set; }
            public string? Direccion { get; set; }
            public string? LocalidadDescripcion { get; set; }
            public string? ProvinciaDescripcion { get; set; }
            public string? ZonaDescripcion { get; set; }
            public string? Email { get; set; }
            public string? Telefono { get; set; }
            public string? Celular { get; set; }
            public string? Contacto { get; set; }
            public string? CondicionIva { get; set; }
            public string? Vendedor { get; set; }
            public int? FkCondIva { get; set; }
            public int? FkLocalidad { get; set; }
            public int? FkVendedor { get; set; }
            public int? FkZona { get; set; }
        }
    }
}
