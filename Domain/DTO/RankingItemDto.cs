using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    public class RankingItemDto
    {
        public string Codigo { get; set; } = "";   // Cod_Prov o Nro (Id cliente) como string
        public string Nombre { get; set; } = "";   // Descripcion producto o Nombre comercial
        public decimal Valor { get; set; }         // Cantidad (productos) o Ventas (clientes)
    }

}
