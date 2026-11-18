using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Promociones.Tests;

/// <summary>Infraestructura de pruebas: base EF en memoria + servicio con las costuras de prueba.</summary>
internal static class Entorno
{
    public static ComercialDbContext NuevaBd()
    {
        var opt = new DbContextOptionsBuilder<ComercialDbContext>()
            .UseInMemoryDatabase("promos-" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ComercialDbContext(opt);
    }

    public static async Task ParametroAsync(ComercialDbContext db, string valor)
    {
        db.Parametros.Add(new Parametro { Modulo = "promociones", Parametro1 = "descuentos", Valor = valor });
        await db.SaveChangesAsync();
    }

    public static async Task<Producto> ProductoAsync(ComercialDbContext db, string desc, decimal precio, bool fraccionado = false)
    {
        var p = new Producto { Descripcion = desc, Baja = false, Fraccionado = fraccionado, CodBarras = "99" + desc.GetHashCode() };
        db.Productos.Add(p);
        await db.SaveChangesAsync();
        db.PreciosProductos.Add(new PreciosProducto { FkProducto = p.Id, Precio = precio });
        await db.SaveChangesAsync();
        return p;
    }

    public static async Task<PromocionRegla> ReglaAsync(ComercialDbContext db, string nombre, string tipo, decimal? pct, int? lleva, int? paga,
        int[] productos, TimeSpan? desde = null, TimeSpan? hasta = null, bool activa = true, bool agrupa = false)
    {
        var ahora = HoraArgentina.Ahora();
        var r = new PromocionRegla
        {
            Nombre = nombre, Tipo = tipo, Porcentaje = pct, Lleva = lleva, Paga = paga, Activa = activa, AgrupaVariantes = agrupa,
            VigenteDesde = ahora + (desde ?? TimeSpan.FromHours(-1)), VigenteHasta = ahora + (hasta ?? TimeSpan.FromHours(1)),
            FechaAlta = ahora
        };
        db.PromocionesReglas.Add(r);
        await db.SaveChangesAsync();
        foreach (var id in productos) db.PromocionesReglasProductos.Add(new PromocionReglaProducto { FkRegla = r.Id, FkProducto = id });
        await db.SaveChangesAsync();
        return r;
    }

    public static PromoServiceTestable Servicio(ComercialDbContext db, bool tablas = true, bool variantes = false)
        => new(db, new EtiquetasFalso(), new VariantesFalso(variantes), tablas);

    /// <summary>Crea las variantes de una ficha (cada variante es un producto con su precio) y devuelve sus ids.</summary>
    public static async Task<int[]> VariantesAsync(ComercialDbContext db, string ficha, decimal precio, params string[] nombres)
    {
        var principal = await ProductoAsync(db, ficha, 0);
        var ids = new List<int>();
        foreach (var n in nombres)
        {
            var v = await ProductoAsync(db, n, precio);
            db.ProductosVariantes.Add(new ProductoVariante { FkProducto = v.Id, FkPrincipal = principal.Id, Activa = true });
            ids.Add(v.Id);
        }
        await db.SaveChangesAsync();
        return ids.ToArray();
    }
}

internal class VariantesFalso : IVariantesProductoService
{
    private readonly bool _habilitado;
    public VariantesFalso(bool habilitado) => _habilitado = habilitado;
    public Task<bool> HabilitadoAsync() => Task.FromResult(_habilitado);
    public Task<HashSet<int>> FichasPrincipalesAsync(IEnumerable<int> productoIds) => Task.FromResult(new HashSet<int>());
    public Task ValidarNoSonFichasAsync(IEnumerable<int> productoIds) => Task.CompletedTask;
}

internal class EtiquetasFalso : IImpresionEtiquetasService
{
    public Task<List<ImpresionEtiquetasItemDto>> BuscarAsync(IEnumerable<int>? p, IEnumerable<int>? r, string? t)
        => Task.FromResult(new List<ImpresionEtiquetasItemDto>());
}

internal class PromoServiceTestable : PromocionDescuentosService
{
    private readonly bool _tablas;
    public PromoServiceTestable(ComercialDbContext db, IImpresionEtiquetasService e, IVariantesProductoService v, bool tablas) : base(db, e, v) => _tablas = tablas;
    protected override bool UsarCache => false;
    protected override Task<int> ContarTablasAsync() => Task.FromResult(_tablas ? 4 : 0);
    protected override Task<bool> ColumnaAgrupaVariantesExisteAsync() => Task.FromResult(true);
}

public class ServicioPromocionesTests
{
    private static PromoGuardarLinea Lin(int i, int prod, decimal cant, decimal precio, decimal descRec = 0, int? regla = null, bool excl = false)
        => new() { Indice = i, FkProducto = prod, Cantidad = cant, PrecioSinIva = precio, DescRec = descRec, FkPromoRegla = regla, Excluida = excl };

    // ── Compatibilidad: motor apagado o sin tablas ──────────────────────────
    [Fact]
    public async Task SinParametro_NoHabilitado_YNoTocaNada()
    {
        await using var db = Entorno.NuevaBd();
        var p = await Entorno.ProductoAsync(db, "A", 100);
        await Entorno.ReglaAsync(db, "20%", "PORC", 20, null, null, new[] { p.Id });
        var s = Entorno.Servicio(db);
        Assert.False(await s.HabilitadoAsync());

        var ev = await s.EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 3, PrecioSinIva = 100 } });
        Assert.False(ev.Habilitado);
        Assert.Empty(ev.Lineas);

        var seg = await s.ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 3, 100) });
        var x = Assert.Single(seg);
        Assert.Null(x.DescuentoPct);          // línea original sin tocar
        Assert.Null(x.Promo);
    }

    [Fact]
    public async Task ParametroEn1_PeroSinTablas_NoHabilitado()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        Assert.False(await Entorno.Servicio(db, tablas: false).HabilitadoAsync());
        Assert.True(await Entorno.Servicio(db, tablas: true).HabilitadoAsync());
    }

    // ── Evaluación ──────────────────────────────────────────────────────────
    [Fact]
    public async Task Evaluar_PorcentajeVigente_DevuelveLineaYResumen()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "PRODUCTO A", 10000);
        var r = await Entorno.ReglaAsync(db, "VERANO", "PORC", 20, null, null, new[] { p.Id });
        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 7, FkProducto = p.Id, Cantidad = 1, PrecioSinIva = 10000 } });

        var l = Assert.Single(ev.Lineas);
        Assert.Equal(7, l.Clave); Assert.Equal(r.Id, l.FkRegla); Assert.Equal(20m, l.DescuentoPct);
        var res = Assert.Single(ev.Resumen);
        Assert.Equal("PRODUCTO A", res.Descripcion);
        Assert.Contains("20% de descuento", res.Texto);
    }

    [Fact]
    public async Task Evaluar_Nxm_ResumenIndicaUnidadesBonificadas()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "B", 100);
        await Entorno.ReglaAsync(db, "2X1", "NXM", null, 2, 1, new[] { p.Id });
        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 4, PrecioSinIva = 100 } });
        var res = Assert.Single(ev.Resumen);
        Assert.Contains("promoción 2x1", res.Texto);
        Assert.Contains("2 unidades bonificadas", res.Texto);
    }

    [Fact]
    public async Task Evaluar_SinPromociones_NoGeneraResumen()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "C", 100);
        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 5, PrecioSinIva = 100 } });
        Assert.True(ev.Habilitado);
        Assert.Empty(ev.Lineas);
        Assert.Empty(ev.Resumen);             // sin promociones: sin mensajes
    }

    [Fact]
    public async Task Vigencia_SeVerificaAlEvaluar_FueraDePeriodoNoAplica()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "D", 100);
        await Entorno.ReglaAsync(db, "VENCIDA", "PORC", 50, null, null, new[] { p.Id }, TimeSpan.FromDays(-3), TimeSpan.FromMinutes(-1));
        await Entorno.ReglaAsync(db, "FUTURA", "PORC", 50, null, null, new[] { p.Id }, TimeSpan.FromMinutes(5), TimeSpan.FromDays(2));
        await Entorno.ReglaAsync(db, "INACTIVA", "PORC", 50, null, null, new[] { p.Id }, activa: false);
        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 1, PrecioSinIva = 100 } });
        Assert.Empty(ev.Lineas);
    }

    [Fact]
    public async Task Fraccionados_QuedanFueraDelMotor()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "QUESO", 100, fraccionado: true);
        await Entorno.ReglaAsync(db, "PORC", "PORC", 30, null, null, new[] { p.Id });
        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 2, PrecioSinIva = 100 } });
        Assert.Empty(ev.Lineas);
    }

    [Fact]
    public async Task Superpuestas_ElServicioEligeLaMasConveniente()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "E", 10000);
        var a = await Entorno.ReglaAsync(db, "A10", "PORC", 10, null, null, new[] { p.Id });
        var b = await Entorno.ReglaAsync(db, "B20", "PORC", 20, null, null, new[] { p.Id });
        var c = await Entorno.ReglaAsync(db, "C2X1", "NXM", null, 2, 1, new[] { p.Id });
        var s = Entorno.Servicio(db);
        var uno  = await s.EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 1, PrecioSinIva = 10000 } });
        var dos  = await s.EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 2, PrecioSinIva = 10000 } });
        Assert.Equal(b.Id, uno.Lineas.Single().FkRegla);
        Assert.Equal(c.Id, dos.Lineas.Single().FkRegla);
    }

    // ── Validación y expansión al guardar ───────────────────────────────────
    [Fact]
    public async Task Guardar_Porcentaje_ConfirmaYDevuelveSegmentoConPromo()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "F", 10000);
        var r = await Entorno.ReglaAsync(db, "20", "PORC", 20, null, null, new[] { p.Id });
        var seg = await Entorno.Servicio(db).ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 2, 10000, -20, r.Id) });
        var x = Assert.Single(seg);
        Assert.Equal(20m, x.DescuentoPct); Assert.Equal(2m, x.Cantidad); Assert.Equal(r.Id, x.Promo!.FkRegla);
        Assert.Equal(4000m, x.Promo.Ahorro);
    }

    [Fact]
    public async Task Guardar_Nxm_ExpandeEnCobradasYBonificadas()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "G", 10000);
        var r = await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, new[] { p.Id });
        // 7 unidades en 3x2: 2 grupos → 2 bonificadas, 5 cobradas → descuento efectivo = 2/7
        var pct = Math.Round(2m / 7m * 100m, 8);
        var seg = await Entorno.Servicio(db).ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 7, 10000, -pct, r.Id) });
        Assert.Equal(2, seg.Count);
        var cobradas = seg[0]; var bonif = seg[1];
        Assert.Equal(5m, cobradas.Cantidad); Assert.Equal(0m, cobradas.DescuentoPct); Assert.False(cobradas.Bonificada);
        Assert.Equal(2m, bonif.Cantidad);    Assert.Equal(100m, bonif.DescuentoPct); Assert.True(bonif.Bonificada);
        Assert.Equal(20000m, bonif.Promo!.Ahorro);
        Assert.Equal(7m, cobradas.Cantidad + bonif.Cantidad);          // no se pierden ni sobran unidades
        Assert.Equal(r.Id, cobradas.Promo!.FkRegla);                   // ambas líneas quedan vinculadas a la regla
    }

    [Fact]
    public async Task Guardar_RechazaPromocionQueYaNoCorresponde()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "H", 100);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Entorno.Servicio(db).ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 2, 100, -20, 999) }));   // dice 20 % pero no hay regla
        Assert.Contains("promociones vigentes cambiaron", ex.Message);
    }

    [Fact]
    public async Task Guardar_RechazaSiFaltaAplicarUnaPromocionVigente()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "I", 100);
        await Entorno.ReglaAsync(db, "10", "PORC", 10, null, null, new[] { p.Id });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Entorno.Servicio(db).ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 1, 100) }));   // el navegador no la aplicó
    }

    [Fact]
    public async Task Guardar_RechazaImporteDistintoAlDelServidor_YReglaDistinta()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "J", 100);
        var r = await Entorno.ReglaAsync(db, "10", "PORC", 10, null, null, new[] { p.Id });
        var s = Entorno.Servicio(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 1, 100, -50, r.Id) }));  // 50 % inventado
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 1, 100, -10, r.Id + 1) })); // regla distinta
    }

    [Fact]
    public async Task Guardar_DescuentoManualGana_YLineasExcluidasNoSeTocan()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "K", 100);
        await Entorno.ReglaAsync(db, "30", "PORC", 30, null, null, new[] { p.Id });
        var s = Entorno.Servicio(db);

        var manual = await s.ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 1, 100, -5) });          // desc/rec manual -5 %, sin promo
        Assert.Null(Assert.Single(manual).DescuentoPct);

        var deUnPedido = await s.ResolverParaGuardarAsync(new[] { Lin(0, p.Id, 1, 100, 0, null, excl: true) });   // precio pactado en un pedido
        Assert.Null(Assert.Single(deUnPedido).DescuentoPct);
    }

    // ── ABM: validaciones y persistencia ────────────────────────────────────
    private static PromoReglaDetalleDto Dto(params int[] prods) => new()
    {
        Nombre = "  promo prueba ", Tipo = "PORC", Porcentaje = 15m, Desde = DateTime.Today, Hasta = DateTime.Today.AddDays(10),
        ProductoIds = prods.ToList()
    };

    [Fact]
    public async Task Abm_GuardaYRecargaConPrecioActual_SinDuplicados()
    {
        await using var db = Entorno.NuevaBd();
        var a = await Entorno.ProductoAsync(db, "PROD A", 10000);
        var b = await Entorno.ProductoAsync(db, "PROD B", 15000);
        var s = Entorno.Servicio(db);
        var id = await s.GuardarAsync(Dto(a.Id, b.Id, a.Id), "tester");     // a repetido: queda una sola vez

        var r = (await s.ObtenerAsync(id))!;
        Assert.Equal("PROMO PRUEBA", r.Nombre);                              // texto en mayúsculas (convención del sistema)
        Assert.Equal(2, r.Productos.Count);
        Assert.Equal(15000m, r.Productos.Single(x => x.ProductoId == b.Id).PrecioActual);

        // el precio no se guarda en la promoción: si cambia el precio original, la promo lo refleja
        db.PreciosProductos.Single(x => x.FkProducto == a.Id).Precio = 12000m;
        await db.SaveChangesAsync();
        Assert.Equal(12000m, (await s.ObtenerAsync(id))!.Productos.Single(x => x.ProductoId == a.Id).PrecioActual);

        // modificar el porcentaje y quitar un producto
        var dto = Dto(a.Id); dto.Id = id; dto.Porcentaje = 25m;
        await s.GuardarAsync(dto, "tester");
        var r2 = (await s.ObtenerAsync(id))!;
        Assert.Equal(25m, r2.Porcentaje); Assert.Single(r2.Productos);

        Assert.True(await s.ToggleActivaAsync(id) == false);
        await s.EliminarAsync(id);
        Assert.Null(await s.ObtenerAsync(id));
        Assert.Empty(db.PromocionesReglasProductos);
    }

    [Fact]
    public async Task Abm_Validaciones()
    {
        await using var db = Entorno.NuevaBd();
        var a = await Entorno.ProductoAsync(db, "PROD", 100);
        var s = Entorno.Servicio(db);
        async Task Falla(PromoReglaDetalleDto d, string texto)
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => s.GuardarAsync(d, null));
            Assert.Contains(texto, ex.Message);
        }
        var d = Dto(a.Id); d.Nombre = "  ";               await Falla(d, "nombre");
        d = Dto(a.Id); d.Porcentaje = 0;                   await Falla(d, "mayor que 0");
        d = Dto(a.Id); d.Porcentaje = 100.01m;             await Falla(d, "mayor que 0");
        d = Dto(a.Id); d.Porcentaje = 10.123m;             await Falla(d, "2 decimales");
        d = Dto(a.Id); d.Hasta = d.Desde.AddMinutes(-1);   await Falla(d, "anterior");
        d = Dto();                                         await Falla(d, "al menos un producto");
        d = Dto(9999);                                     await Falla(d, "ya no existen");
        d = Dto(a.Id); d.Tipo = "NXM"; d.Lleva = 2; d.Paga = 2;   await Falla(d, "menor que la que lleva");
        d = Dto(a.Id); d.Tipo = "NXM"; d.Lleva = 1; d.Paga = 1;   await Falla(d, "entre 2 y 99");
        d = Dto(a.Id); d.Tipo = "NXM"; d.Lleva = 3; d.Paga = 0;   await Falla(d, "al menos 1");
        d = Dto(a.Id); d.Tipo = "XXX";                     await Falla(d, "Tipo");
        d = Dto(a.Id); d.Tipo = "NXM"; d.Lleva = 4; d.Paga = 3; d.Porcentaje = null;
        Assert.True(await s.GuardarAsync(d, null) > 0);    // 4x3 válido
    }

    [Fact]
    public async Task Abm_SinTablas_NoPermiteGuardarYListaVacia()
    {
        await using var db = Entorno.NuevaBd();
        var s = Entorno.Servicio(db, tablas: false);
        Assert.Empty(await s.ListarAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => s.GuardarAsync(Dto(1), null));
    }
}
