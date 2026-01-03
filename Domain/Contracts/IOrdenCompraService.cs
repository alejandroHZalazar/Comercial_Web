using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contracts
{
    public interface IOrdenCompraService
    {
        Task<List<OrdenCompra>> GetOrdenesAsync();
        Task<OrdenCompra?> GetOrdenByIdAsync(long id);
        Task<List<OrdenCompraDetalle>> GetDetallesByOrdenIdAsync(long ordenId);

        Task<long> CrearOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles);
        Task ActualizarOrdenAsync(OrdenCompra orden, List<OrdenCompraDetalle> detalles);
        Task EliminarOrdenAsync(long id); // baja lógica o eliminación física
    }
}
