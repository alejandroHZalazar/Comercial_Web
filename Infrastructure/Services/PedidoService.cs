using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PedidoService : IPedidoService
{
    private readonly ComercialDbContext _db;

    public PedidoService(ComercialDbContext db) => _db = db;

    // ── Autocomplete clientes ────────────────────────────────────────────
    public async Task<List<PedidoClienteItem>> BuscarClientesAsync(string q)
    {
        var qUpper = q.Trim().ToUpperInvariant();
        var rows = await (
            from c    in _db.Clientes
            where c.Baja != true
               && c.NombreComercial != null
               && c.NombreComercial.Contains(qUpper)
            join l    in _db.Localidades on c.FkLocalidad equals l.Id    into lj
            from l    in lj.DefaultIfEmpty()
            join prov in _db.Provincias  on (int?)l.FkProvincia equals prov.Id into pj
            from prov in pj.DefaultIfEmpty()
            orderby c.NombreComercial
            select new
            {
                c.Id, c.NombreComercial, c.Telefono, c.Contacto, c.Direccion,
                LocalidadNombre = l    != null ? l.Nombre    : null,
                ProvinciaNombre = prov != null ? prov.Nombre : null
            }
        ).Take(20).ToListAsync();

        return rows.Select(x => new PedidoClienteItem
        {
            ClienteId       = x.Id,
            NombreComercial = x.NombreComercial,
            Telefono        = x.Telefono,
            Contacto        = x.Contacto,
            DireccionFull   = string.Join(", ",
                new[] { x.Direccion, x.LocalidadNombre, x.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
        }).ToList();
    }

    public async Task<PedidoClienteItem?> GetClientePorIdAsync(int clienteId)
    {
        var row = await (
            from c    in _db.Clientes
            where c.Id == clienteId && c.Baja != true
            join l    in _db.Localidades on c.FkLocalidad equals l.Id   into lj
            from l    in lj.DefaultIfEmpty()
            join prov in _db.Provincias  on (int?)l.FkProvincia equals prov.Id into pj
            from prov in pj.DefaultIfEmpty()
            select new
            {
                c.Id, c.NombreComercial, c.Telefono, c.Contacto, c.Direccion,
                LocalidadNombre = l    != null ? l.Nombre    : null,
                ProvinciaNombre = prov != null ? prov.Nombre : null
            }
        ).FirstOrDefaultAsync();

        if (row == null) return null;

        return new PedidoClienteItem
        {
            ClienteId       = row.Id,
            NombreComercial = row.NombreComercial,
            Telefono        = row.Telefono,
            Contacto        = row.Contacto,
            DireccionFull   = string.Join(", ",
                new[] { row.Direccion, row.LocalidadNombre, row.ProvinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
        };
    }

    // ── Buscar pedidos (emula sp_pedidosTraerParaEditar) ─────────────────
    public async Task<List<PedidoBuscarItem>> BuscarPedidosAsync(
        DateTime? desde, DateTime? hasta,
        List<int> vendedores, List<int> clientes)
    {
        var q = from p in _db.Pedidos
                where p.Total > 0
                join c in _db.Clientes on p.FkCliente equals c.Id
                join u in _db.Usuarios on p.FkVendedor equals u.Id
                select new { p, ClienteNombre = c.NombreComercial, VendedorNombre = u.Nombre, p.Impreso, p.Vendido };

        if (desde.HasValue)
            q = q.Where(x => x.p.Fecha >= desde.Value);
        if (hasta.HasValue)
            q = q.Where(x => x.p.Fecha <= hasta.Value.Date.AddDays(1).AddTicks(-1));
        if (vendedores.Count > 0)
            q = q.Where(x => x.p.FkVendedor.HasValue && vendedores.Contains(x.p.FkVendedor.Value));
        if (clientes.Count > 0)
            q = q.Where(x => x.p.FkCliente.HasValue && clientes.Contains(x.p.FkCliente.Value));

        return await q
            .OrderByDescending(x => x.p.Fecha)
            .Take(300)
            .Select(x => new PedidoBuscarItem
            {
                Id             = x.p.Id,
                Fecha          = x.p.Fecha,
                NombreCliente  = x.ClienteNombre,
                FkCliente      = x.p.FkCliente,
                NombreVendedor = x.VendedorNombre,
                Observacion    = x.p.Observacion,
                Iva            = x.p.Iva,
                Total          = x.p.Total,
                Impreso        = x.Impreso,
                Vendido        = x.Vendido
            }).ToListAsync();
    }

    // ── Obtener cabecera de pedido para editar ───────────────────────────
    public async Task<PedidoCabeceraDto?> GetCabeceraAsync(int pedidoId)
    {
        // Se resuelve en dos pasos para evitar joins condicionales no traducibles por EF Core
        var pedidoRow = await (
            from p in _db.Pedidos
            where p.Id == pedidoId
            join c in _db.Clientes on p.FkCliente equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            select new
            {
                p.Id, p.FkCliente, p.Iva, p.FkVendedor, p.Observacion, p.Total, p.Fecha,
                p.Descuento, p.Recargo,
                NombreComercial = c != null ? c.NombreComercial : null,
                Telefono        = c != null ? c.Telefono        : null,
                Contacto        = c != null ? c.Contacto        : null,
                Direccion       = c != null ? c.Direccion       : null,
                FkLocalidad     = c != null ? c.FkLocalidad     : (int?)null
            }
        ).FirstOrDefaultAsync();

        if (pedidoRow == null) return null;

        string? localidadNombre = null;
        string? provinciaNombre = null;

        if (pedidoRow.FkLocalidad.HasValue)
        {
            var loc = await _db.Localidades.FirstOrDefaultAsync(l => l.Id == pedidoRow.FkLocalidad.Value);
            if (loc != null)
            {
                localidadNombre = loc.Nombre;
                if (loc.FkProvincia.HasValue)
                {
                    var prov = await _db.Provincias.FirstOrDefaultAsync(p => p.Id == loc.FkProvincia.Value);
                    provinciaNombre = prov?.Nombre;
                }
            }
        }

        return new PedidoCabeceraDto
        {
            Id              = pedidoRow.Id,
            FkCliente       = pedidoRow.FkCliente,
            Iva             = pedidoRow.Iva,
            FkVendedor      = pedidoRow.FkVendedor,
            Observacion     = pedidoRow.Observacion,
            Total           = pedidoRow.Total,
            Fecha           = pedidoRow.Fecha,
            Descuento       = pedidoRow.Descuento,
            Recargo         = pedidoRow.Recargo,
            NombreComercial = pedidoRow.NombreComercial,
            Telefono        = pedidoRow.Telefono,
            Contacto        = pedidoRow.Contacto,
            DireccionFull   = string.Join(", ",
                new[] { pedidoRow.Direccion, localidadNombre, provinciaNombre }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
        };
    }

    // ── Detalle de pedido (emula sp_PedidosTraerDetalleParaEditar) ────────
    public async Task<List<PedidoDetalleItemDto>> GetDetalleAsync(int pedidoId)
    {
        var rows = await (
            from pd   in _db.PedidoDetalles
            where pd.FkPedido == pedidoId
            join s    in _db.StockProductos on pd.FkProducto equals s.FkProducto into sj
            from s    in sj.DefaultIfEmpty()
            join prod in _db.Productos      on pd.FkProducto equals prod.Id       into pj
            from prod in pj.DefaultIfEmpty()
            orderby pd.CodProveedor
            select new
            {
                pd.FkProducto, pd.CodBarras, pd.CodProveedor, pd.Descripcion, pd.Observ,
                Stock          = s    != null ? (s.Cantidad    ?? 0m) : 0m,
                pd.PrecioSinIva, pd.PrecioConIva, pd.PrecioOrig, pd.Costo, pd.Cantidad,
                Descuento      = pd.Descuento     ?? 0m,
                Recargo        = pd.Recargo       ?? 0m,
                SubtotalSinIva = pd.SubtotalSinIva ?? 0m,
                Subtotal       = pd.Subtotal       ?? 0m,
                Fraccionado    = prod != null && (prod.Fraccionado ?? false),
                Dolarizado     = prod != null && (prod.Dolarizado  ?? false)
            }
        ).ToListAsync();

        return rows.Select(r => new PedidoDetalleItemDto
        {
            FkProducto     = r.FkProducto     ?? 0,
            CodBarras      = r.CodBarras,
            CodProveedor   = r.CodProveedor,
            Descripcion    = r.Descripcion,
            Observ         = r.Observ,
            Stock          = r.Stock,
            PrecioSinIva   = r.PrecioSinIva   ?? 0m,
            PrecioConIva   = r.PrecioConIva   ?? 0m,
            PrecioOrig     = r.PrecioOrig     ?? 0m,
            Costo          = r.Costo          ?? 0m,
            Cantidad       = r.Cantidad       ?? 0m,
            Descuento      = r.Descuento,
            Recargo        = r.Recargo,
            SubtotalSinIva = r.SubtotalSinIva,
            Subtotal       = r.Subtotal,
            Fraccionado    = r.Fraccionado,
            Dolarizado     = r.Dolarizado
        }).ToList();
    }

    // ── Búsqueda de productos ────────────────────────────────────────────
    public async Task<List<PedidoProductoItem>> BuscarProductosAsync(
        string q, string tipo,
        bool dolarizaProductos, decimal cotizDolar,
        List<int>? proveedores = null)
    {
        IQueryable<Producto> query = tipo.ToLower() switch
        {
            "codbarras"    => _db.Productos.Where(p => p.Baja != true && p.CodBarras    == q),
            "codproveedor" => _db.Productos.Where(p => p.Baja != true && p.CodProveedor == q),
            _              => _db.Productos.Where(p => p.Baja != true
                                && p.Descripcion != null
                                && p.Descripcion.Contains(q.ToUpperInvariant()))
        };

        if (proveedores != null && proveedores.Count > 0)
        {
            query = query.Where(p => p.FkProveedor.HasValue && proveedores.Contains(p.FkProveedor.Value));
        }

        var prods = await query.OrderBy(p => p.Descripcion).Take(30).ToListAsync();
        if (prods.Count == 0) return new List<PedidoProductoItem>();

        var ids = prods.Select(p => p.Id).ToList();

        var precios = await _db.PreciosProductos
            .Where(pp => ids.Contains(pp.FkProducto ?? 0))
            .ToDictionaryAsync(pp => pp.FkProducto ?? 0, pp => pp.Precio ?? 0m);

        var costos = await _db.CostosProductos
            .Where(cp => ids.Contains(cp.FkProducto ?? 0))
            .ToDictionaryAsync(cp => cp.FkProducto ?? 0, cp => cp.Costo ?? 0m);

        var stocks = await _db.StockProductos
            .Where(s => ids.Contains(s.FkProducto ?? 0))
            .ToDictionaryAsync(s => s.FkProducto ?? 0, s => s.Cantidad ?? 0m);

        return prods.Select(p =>
        {
            var precioBase = precios.GetValueOrDefault(p.Id, 0m);
            if (dolarizaProductos && (p.Dolarizado ?? false) && cotizDolar > 0)
                precioBase = precioBase * cotizDolar;

            return new PedidoProductoItem
            {
                ProductoId   = p.Id,
                CodBarras    = p.CodBarras,
                CodProveedor = p.CodProveedor,
                Descripcion  = p.Descripcion,
                Stock        = stocks.GetValueOrDefault(p.Id, 0m),
                PrecioSinIva = precioBase,
                PrecioConIva = precioBase,   // IVA se aplica del lado del cliente con el combo IVA
                PrecioOrig   = precios.GetValueOrDefault(p.Id, 0m),
                Costo        = costos.GetValueOrDefault(p.Id, 0m),
                Fraccionado  = p.Fraccionado  ?? false,
                Dolarizado   = p.Dolarizado   ?? false,
                EsPromocion  = p.EsPromocion  ?? false
            };
        }).ToList();
    }

    // ── Guardar / actualizar pedido ──────────────────────────────────────
    public async Task<int> GuardarPedidoAsync(GuardarPedidoRequestDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            Pedido pedido;
            if (dto.PedidoId > 0)
            {
                pedido = await _db.Pedidos.FindAsync(dto.PedidoId)
                    ?? throw new Exception($"Pedido {dto.PedidoId} no encontrado.");
                pedido.Total       = dto.Total;
                pedido.FkCliente   = dto.FkCliente;
                pedido.Iva         = dto.Iva;
                pedido.FkVendedor  = dto.FkVendedor;
                pedido.Observacion = dto.Observacion;

                var detalleViejo = _db.PedidoDetalles.Where(d => d.FkPedido == dto.PedidoId);
                _db.PedidoDetalles.RemoveRange(detalleViejo);
                await _db.SaveChangesAsync();
            }
            else
            {
                pedido = new Pedido
                {
                    Total       = dto.Total,
                    Fecha       = DateTime.Now,
                    FkCliente   = dto.FkCliente,
                    Iva         = dto.Iva,
                    Recargo     = null,
                    Descuento   = null,
                    FkVendedor  = dto.FkVendedor,
                    Observacion = dto.Observacion,
                    Impreso     = false,
                    Vendido     = false
                };
                _db.Pedidos.Add(pedido);
                await _db.SaveChangesAsync();
            }

            foreach (var item in dto.Detalle)
            {
                _db.PedidoDetalles.Add(new PedidoDetalle
                {
                    FkPedido       = pedido.Id,
                    FkProducto     = item.FkProducto,
                    CodBarras      = item.CodBarras,
                    CodProveedor   = item.CodProveedor,
                    Descripcion    = item.Descripcion,
                    PrecioSinIva   = item.PrecioSinIva,
                    Cantidad       = item.Cantidad,
                    Subtotal       = item.Subtotal,   // preserva importe fraccionado exacto
                    Procesado      = false,
                    CantEntregada  = 0,
                    PrecioOrig     = item.PrecioOrig,
                    Costo          = item.Costo,
                    PrecioConIva   = item.PrecioConIva,
                    FkColor        = 0,
                    Observ         = item.Observ,
                    Descuento      = item.Descuento > 0 ? item.Descuento : (decimal?)null,
                    Recargo        = item.Recargo   > 0 ? item.Recargo   : (decimal?)null,
                    SubtotalSinIva = item.SubtotalSinIva
                });
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return pedido.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Crear cliente rápido ─────────────────────────────────────────────
    public async Task<(int clienteId, string nombreComercial, string? telefono, string? contacto, string? direccion)>
        CrearClienteRapidoAsync(CrearClienteRapidoDto dto)
    {
        var nombreUp   = dto.NombreComercial.Trim().ToUpperInvariant();
        var razonUp    = dto.RazonSocial?.Trim().ToUpperInvariant();
        var cuilUp     = dto.Cuil?.Trim().ToUpperInvariant();
        var dirUp      = dto.Direccion?.Trim().ToUpperInvariant();
        var contactoUp = dto.Contacto?.Trim().ToUpperInvariant();

        var cliente = new Cliente
        {
            NombreComercial = nombreUp,
            RazonSocial     = razonUp,
            Cuil            = cuilUp,
            Direccion       = dirUp,
            Email           = dto.Email?.Trim(),
            Telefono        = dto.Telefono?.Trim(),
            Celular         = dto.Celular?.Trim(),
            Contacto        = contactoUp,
            FkCondIva       = dto.FkCondIva > 0 ? dto.FkCondIva : null,
            FkVendedor      = dto.FkVendedor > 0 ? dto.FkVendedor : null,
            FkLocalidad     = dto.FkLocalidad > 0 ? dto.FkLocalidad : null,
            FkZona          = dto.FkZona > 0 ? dto.FkZona : null,
            Baja            = false
        };
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();
        return (cliente.Id, nombreUp, cliente.Telefono, contactoUp, dirUp);
    }

    // ── Marcar pedido como impreso ───────────────────────────────────────
    public async Task MarcarImpresoAsync(int pedidoId)
    {
        var pedido = await _db.Pedidos.FindAsync(pedidoId);
        if (pedido == null) return;
        pedido.Impreso = true;
        await _db.SaveChangesAsync();
    }

    // ── Marcar pedido como vendido ───────────────────────────────────────
    public async Task MarcarVendidoAsync(int pedidoId)
    {
        var pedido = await _db.Pedidos.FindAsync(pedidoId);
        if (pedido == null) return;
        pedido.Vendido = true;
        await _db.SaveChangesAsync();
    }
}
