using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class ProveedorCabeceraDto
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = "";
        public string Direccion { get; set; } = "";
    }

}
