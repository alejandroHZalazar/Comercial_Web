using Domain.DTO;

namespace Domain.Contracts;

public interface IPedidoService
{
    /// <summary>Autocomplete de clientes por nombre comercial.</summary>
    Task<List<PedidoClienteItem>> BuscarClientesAsync(string q);

    /// <summary>Obtiene un cliente por ID (para cargar consumidor final automático).</summary>
    Task<PedidoClienteItem?> GetClientePorIdAsync(int clienteId);

    /// <summary>Busca pedidos con filtros (emula sp_pedidosTraerParaEditar).</summary>
    Task<List<PedidoBuscarItem>> BuscarPedidosAsync(
        DateTime? desde, DateTime? hasta,
        List<int> vendedores, List<int> clientes);

    /// <summary>Trae la cabecera + datos del cliente de un pedido.</summary>
    Task<PedidoCabeceraDto?> GetCabeceraAsync(int pedidoId);

    /// <summary>Trae el detalle de un pedido (emula sp_PedidosTraerDetalleParaEditar).</summary>
    Task<List<PedidoDetalleItemDto>> GetDetalleAsync(int pedidoId);

    /// <summary>Búsqueda de productos. tipo: "codBarras" | "codProveedor" | "descripcion".</summary>
    Task<List<PedidoProductoItem>> BuscarProductosAsync(
        string q, string tipo,
        bool dolarizaProductos, decimal cotizDolar,
        List<int>? proveedores = null);

    /// <summary>Guarda o actualiza un pedido completo.</summary>
    Task<int> GuardarPedidoAsync(GuardarPedidoRequestDto dto);

    /// <summary>Crea un cliente básico desde el formulario de pedidos.</summary>
    Task<(int clienteId, string nombreComercial, string? telefono, string? contacto, string? direccion)> CrearClienteRapidoAsync(CrearClienteRapidoDto dto);

    /// <summary>Marca un pedido como impreso.</summary>
    Task MarcarImpresoAsync(int pedidoId);

    /// <summary>Marca un pedido como vendido (pedido.vendido = true).</summary>
    Task MarcarVendidoAsync(int pedidoId);
}
