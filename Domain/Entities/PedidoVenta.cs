using System;

namespace Domain.Entities;

public partial class PedidoVenta
{
    public int Id { get; set; }

    public int FkPedido { get; set; }

    public long FkVenta { get; set; }

    public DateTime Fecha { get; set; }
}
