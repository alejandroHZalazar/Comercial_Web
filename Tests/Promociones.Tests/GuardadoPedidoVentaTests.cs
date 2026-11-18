using Domain.DTO;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Promociones.Tests;

/// <summary>Guardado real de pedidos y ventas (servicios de producción) sobre una base en memoria.</summary>
public class GuardadoPedidoVentaTests
{
    private const decimal Iva = 21m;

    private static PedidoService Pedidos(ComercialDbContext db) =>
        new(db, new VariantesProductoService(db), Entorno.Servicio(db));

    private static VentaService Ventas(ComercialDbContext db) =>
        new(db, null!, new VariantesProductoService(db), Entorno.Servicio(db));

    private static GuardarPedidoDetalleDto LineaPedido(int prod, decimal cant, decimal precio, decimal descuentoPct = 0, int? regla = null)
    {
        var subSin = precio * (1m - descuentoPct / 100m);
        var conIva = subSin * (1m + Iva / 100m);
        return new GuardarPedidoDetalleDto
        {
            FkProducto = prod, Cantidad = cant, PrecioSinIva = precio, PrecioOrig = precio, Costo = 1, Descripcion = "X",
            Descuento = descuentoPct, SubtotalSinIva = subSin, PrecioConIva = conIva, Subtotal = conIva * cant, FkPromoRegla = regla
        };
    }

    private static GuardarPedidoRequestDto Pedido(params GuardarPedidoDetalleDto[] l) =>
        new() { FkCliente = 1, Iva = Iva, FkVendedor = 1, Total = l.Sum(x => x.Subtotal), Detalle = l.ToList() };

    // ══ PEDIDOS ═════════════════════════════════════════════════════════════
    [Fact]
    public async Task Pedido_Nxm_PersisteCobradasYBonificadas_ConAuditoria_YTotalIgual()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "SHAMPOO", 10000);
        var r = await Entorno.ReglaAsync(db, "2X1", "NXM", null, 2, 1, new[] { p.Id });

        // 4 unidades con 2x1: el navegador informa un 50 % efectivo
        var dto = Pedido(LineaPedido(p.Id, 4, 10000, 50, r.Id));
        var totalCliente = dto.Total;                                    // 2 × 10.000 × 1,21 = 24.200
        var id = await Pedidos(db).GuardarPedidoAsync(dto);

        var lineas = await db.PedidoDetalles.Where(d => d.FkPedido == id).OrderBy(d => d.Linea).ToListAsync();
        Assert.Equal(2, lineas.Count);
        Assert.Equal(2m, lineas[0].Cantidad); Assert.Null(lineas[0].Descuento);        // cobradas a precio normal
        Assert.Equal(2m, lineas[1].Cantidad); Assert.Equal(100m, lineas[1].Descuento); // bonificadas al 100 %
        Assert.Equal(0m, lineas[1].Subtotal);
        Assert.Equal(4m, lineas.Sum(l => l.Cantidad));                                  // no sobran ni faltan unidades
        Assert.Equal(totalCliente, lineas.Sum(l => l.Subtotal));                        // total de líneas = total informado
        Assert.Equal(24200m, lineas.Sum(l => l.Subtotal));

        var aud = await db.PedidosPromoDetalle.OrderBy(a => a.FkPedidoDetalle).ToListAsync();
        Assert.Equal(2, aud.Count);                                                     // las dos líneas quedan vinculadas
        Assert.All(aud, a => { Assert.Equal(r.Id, a.FkRegla); Assert.Equal("2X1", a.Nombre); Assert.Equal("NXM", a.Tipo); });
        Assert.Equal(20000m, aud.Sum(a => a.Ahorro));

        // Recarga: ambas líneas informan su promoción (el navegador las reúne en una sola fila)
        var det = await Pedidos(db).GetDetalleAsync(id);
        Assert.All(det, d => Assert.Equal(r.Id, d.FkPromoRegla));
    }

    [Fact]
    public async Task Pedido_Porcentaje_UnaLineaConSuDescuento()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "PERFUME", 8000);
        var r = await Entorno.ReglaAsync(db, "30", "PORC", 30, null, null, new[] { p.Id });
        var id = await Pedidos(db).GuardarPedidoAsync(Pedido(LineaPedido(p.Id, 2, 8000, 30, r.Id)));

        var l = Assert.Single(await db.PedidoDetalles.Where(d => d.FkPedido == id).ToListAsync());
        Assert.Equal(2m, l.Cantidad); Assert.Equal(30m, l.Descuento);
        Assert.Equal(Math.Round(2 * 8000m * 0.7m * 1.21m, 2), Math.Round(l.Subtotal!.Value, 2));
        var a = Assert.Single(await db.PedidosPromoDetalle.ToListAsync());
        Assert.Equal(4800m, a.Ahorro); Assert.Null(a.CantBonificada);
    }

    [Fact]
    public async Task Pedido_RechazaPrecioPromocionalInventadoPorElNavegador()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "SHAMPOO", 10000);
        var r = await Entorno.ReglaAsync(db, "10", "PORC", 10, null, null, new[] { p.Id });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Pedidos(db).GuardarPedidoAsync(Pedido(LineaPedido(p.Id, 1, 10000, 90, r.Id))));      // 90 % inventado
        Assert.Empty(db.Pedidos);                                                                // nada se guardó
    }

    [Fact]
    public async Task Pedido_SinPromociones_GuardaIgualQueAntes()
    {
        await using var db = Entorno.NuevaBd();                          // sin parámetro: motor apagado
        var p = await Entorno.ProductoAsync(db, "NORMAL", 500);
        await Entorno.ReglaAsync(db, "30", "PORC", 30, null, null, new[] { p.Id });   // aunque exista la regla, no se aplica
        var id = await Pedidos(db).GuardarPedidoAsync(Pedido(LineaPedido(p.Id, 3, 500)));
        var l = Assert.Single(await db.PedidoDetalles.Where(d => d.FkPedido == id).ToListAsync());
        Assert.Equal(3m, l.Cantidad); Assert.Null(l.Descuento);
        Assert.Empty(db.PedidosPromoDetalle);
        Assert.All(await Pedidos(db).GetDetalleAsync(id), d => Assert.Null(d.FkPromoRegla));
    }

    [Fact]
    public async Task Pedido_ProductoSinPromocion_OtrosProductosSinCambios()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var conPromo = await Entorno.ProductoAsync(db, "CON", 100);
        var sinPromo = await Entorno.ProductoAsync(db, "SIN", 200);
        var r = await Entorno.ReglaAsync(db, "50", "PORC", 50, null, null, new[] { conPromo.Id });
        var id = await Pedidos(db).GuardarPedidoAsync(Pedido(LineaPedido(conPromo.Id, 1, 100, 50, r.Id), LineaPedido(sinPromo.Id, 5, 200)));
        var ls = await db.PedidoDetalles.Where(d => d.FkPedido == id).OrderBy(d => d.Linea).ToListAsync();
        Assert.Equal(2, ls.Count);
        Assert.Null(ls[1].Descuento); Assert.Equal(5m, ls[1].Cantidad);
        Assert.Single(db.PedidosPromoDetalle);
    }

    [Fact]
    public async Task Pedido_Edicion_ReemplazaLineasYAuditoria()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "ED", 100);
        var r = await Entorno.ReglaAsync(db, "2X1", "NXM", null, 2, 1, new[] { p.Id });
        var svc = Pedidos(db);
        var id = await svc.GuardarPedidoAsync(Pedido(LineaPedido(p.Id, 2, 100, 50, r.Id)));
        Assert.Equal(2, db.PedidoDetalles.Count());

        var dto = Pedido(LineaPedido(p.Id, 1, 100));                      // se edita: queda 1 unidad, sin promoción
        dto.PedidoId = id;
        await svc.GuardarPedidoAsync(dto);
        var ls = await db.PedidoDetalles.Where(d => d.FkPedido == id).ToListAsync();
        Assert.Single(ls); Assert.Equal(1m, ls[0].Cantidad);
    }

    // ══ VENTAS ══════════════════════════════════════════════════════════════
    private static GrabarVentaDetalleDto LineaVenta(int prod, decimal cant, decimal precio, decimal descRec = 0, int? regla = null, bool bloqueada = false)
    {
        var subSin = precio * (1m + descRec / 100m);
        var conIva = subSin * (1m + Iva / 100m);
        return new GrabarVentaDetalleDto
        {
            FkProducto = prod, Cantidad = cant, PrecioSinIva = precio, PrecioConIva = conIva, Costo = 1, DescRec = descRec,
            SubtotalSinIva = subSin, Subtotal = conIva * cant, FkPromoRegla = regla, PromoBloqueada = bloqueada
        };
    }

    private static GrabarVentaRequestDto Venta(params GrabarVentaDetalleDto[] l) =>
        new() { FkCliente = 1, Iva = Iva, FkVendedor = 1, Total = l.Sum(x => x.Subtotal), Costo = 1, Detalle = l.ToList() };

    [Fact]
    public async Task Venta_Nxm_PersisteCobradasYBonificadas_BajaStockCompleto_YAuditoria()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "SHAMPOO", 10000);
        db.StockProductos.Add(new Domain.Entities.StockProducto { FkProducto = p.Id, Cantidad = 100 });
        await db.SaveChangesAsync();
        var r = await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, new[] { p.Id });

        // 6 unidades 3x2: 2 grupos → 2 bonificadas → 4 cobradas, efectivo 33,33333333 %
        var pct = Math.Round(2m / 6m * 100m, 8);
        var dto = Venta(LineaVenta(p.Id, 6, 10000, -pct, r.Id));
        var id = await Ventas(db).GrabarVentaAsync(dto);

        var ls = await db.VentasDetalles.Where(d => d.FkVenta == id).OrderBy(d => d.Linea).ToListAsync();
        Assert.Equal(2, ls.Count);
        Assert.Equal(4m, ls[0].Cantidad); Assert.Null(ls[0].Descuento);
        Assert.Equal(2m, ls[1].Cantidad); Assert.Equal(100m, ls[1].Descuento); Assert.Equal(0m, ls[1].Subtotal);
        Assert.Equal(Math.Round(4 * 10000m * 1.21m, 4), Math.Round(ls.Sum(l => l.Subtotal!.Value), 4));   // 48.400
        Assert.Equal(94m, (await db.StockProductos.SingleAsync(s => s.FkProducto == p.Id)).Cantidad);       // salen las 6 unidades
        Assert.Equal(2, db.ProductosMovimientos.Count(m => m.FkProducto == p.Id));

        var aud = await db.VentasPromoDetalle.ToListAsync();
        Assert.Equal(2, aud.Count);
        Assert.Equal(20000m, aud.Sum(a => a.Ahorro));
        Assert.All(aud, a => Assert.Equal(r.Id, a.FkRegla));
    }

    [Fact]
    public async Task Venta_Porcentaje_YRechazoDeImportesQueNoCoinciden()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "PERFUME", 8000);
        var r = await Entorno.ReglaAsync(db, "15", "PORC", 15, null, null, new[] { p.Id });

        var id = await Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 2, 8000, -15, r.Id)));
        var l = Assert.Single(await db.VentasDetalles.Where(d => d.FkVenta == id).ToListAsync());
        Assert.Equal(15m, l.Descuento);
        Assert.Equal(Math.Round(2 * 8000m * 0.85m * 1.21m, 4), Math.Round(l.Subtotal!.Value, 4));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 1, 8000, -60, r.Id))));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 1, 8000))));        // promo vigente no aplicada
    }

    [Fact]
    public async Task Venta_LineaBloqueadaDePedido_NoSeValidaNiSeExpande()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "PACTADO", 100);
        await Entorno.ReglaAsync(db, "50", "PORC", 50, null, null, new[] { p.Id });
        var id = await Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 2, 100, 0, null, bloqueada: true)));   // precio del pedido
        var l = Assert.Single(await db.VentasDetalles.Where(d => d.FkVenta == id).ToListAsync());
        Assert.Equal(2m, l.Cantidad); Assert.Null(l.Descuento);
        Assert.Empty(db.VentasPromoDetalle);
    }

    [Fact]
    public async Task Venta_SinPromociones_ComportamientoActualIntacto()
    {
        await using var db = Entorno.NuevaBd();                          // motor apagado
        var p = await Entorno.ProductoAsync(db, "NORMAL", 100);
        await Entorno.ReglaAsync(db, "50", "PORC", 50, null, null, new[] { p.Id });
        var id = await Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 3, 100, -10)));      // desc. manual de línea 10 %
        var l = Assert.Single(await db.VentasDetalles.Where(d => d.FkVenta == id).ToListAsync());
        Assert.Equal(3m, l.Cantidad); Assert.Equal(10m, l.Descuento);
        Assert.Empty(db.VentasPromoDetalle);
    }

    [Fact]
    public async Task Venta_DescuentoManualGanaSobrePromocion()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "MANUAL", 100);
        await Entorno.ReglaAsync(db, "50", "PORC", 50, null, null, new[] { p.Id });
        var id = await Ventas(db).GrabarVentaAsync(Venta(LineaVenta(p.Id, 2, 100, -5)));       // 5 % manual, sin promoción declarada
        var l = Assert.Single(await db.VentasDetalles.Where(d => d.FkVenta == id).ToListAsync());
        Assert.Equal(5m, l.Descuento);
        Assert.Empty(db.VentasPromoDetalle);
    }
}
