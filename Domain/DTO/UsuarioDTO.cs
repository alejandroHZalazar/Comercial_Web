using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class UsuarioDTO
    {
        public class UsuarioGridABMItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string TipoDescripcion { get; set; } = string.Empty;
        }
    }
}
