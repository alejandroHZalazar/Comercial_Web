using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Promociones.Tests;

/// <summary>Generador con las costuras de prueba: EF en memoria no soporta SQL ni transacciones reales.</summary>
internal class GeneradorTestable : GeneradorCodigoBarrasService
{
    private readonly ComercialDbContext _bd;
    private readonly bool _transaccion;
    public GeneradorTestable(ComercialDbContext db, bool transaccion = true) : base(db) { _bd = db; _transaccion = transaccion; }
    protected override bool HayTransaccion => _transaccion;
    protected override async Task<string?> LeerContadorBloqueandoAsync()
        => (await _bd.Parametros.FirstOrDefaultAsync(p => p.Modulo == "productos" && p.Parametro1 == "proximoCodigoBarras"))?.Valor;
    protected override async Task GuardarContadorAsync(string valor)
    {
        (await _bd.Parametros.FirstAsync(p => p.Modulo == "productos" && p.Parametro1 == "proximoCodigoBarras")).Valor = valor;
        await _bd.SaveChangesAsync();
    }
}

public class CodigoBarrasTests
{
    private static async Task ParametrosAsync(ComercialDbContext db, string habilitado = "1", string? proximo = "1")
    {
        db.Parametros.Add(new Parametro { Id = 1001, Modulo = "productos", Parametro1 = "generaCodigoBarras", Valor = habilitado });
        if (proximo != null)
            db.Parametros.Add(new Parametro { Id = 1002, Modulo = "productos", Parametro1 = "proximoCodigoBarras", Valor = proximo });
        await db.SaveChangesAsync();
    }

    private static async Task ExistenteAsync(ComercialDbContext db, string? barras, string? prov, bool baja = false)
    {
        db.Productos.Add(new Producto { Descripcion = "X", CodBarras = barras, CodProveedor = prov, Baja = baja });
        await db.SaveChangesAsync();
    }

    private static string Contador(ComercialDbContext db)
        => db.Parametros.Single(p => p.Parametro1 == "proximoCodigoBarras").Valor!;

    private static ProductoService Productos(ComercialDbContext db)
        => new(db, new VariantesFalso(true), new GeneradorTestable(db));

    private static ProductoDetallesDTO Dto(string? barras = null, string? prov = null) => new()
    {
        Descripcion = "PRODUCTO NUEVO", CodBarras = barras, CodProveedor = prov, Precio = 100, Costo = 50, Cantidad = 0,
        FkRubro = 1, FkProveedor = 1
    };

    // ── Generador ───────────────────────────────────────────────────────────
    [Fact]
    public async Task EmpiezaEn001_YSigueCorrelativo()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        var g = new GeneradorTestable(db);
        Assert.Equal(new[] { "001", "002", "003" }, await g.ReservarAsync(3));
        Assert.Equal("4", Contador(db));
        Assert.Equal(new[] { "004" }, await g.ReservarAsync(1));
    }

    [Fact]
    public async Task SaltaCodigosUsadosComoBarrasOProveedor_InclusoDeBaja()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        await ExistenteAsync(db, "001", null);               // como código de barras
        await ExistenteAsync(db, null, "002");               // como código de proveedor
        await ExistenteAsync(db, "003", "003", baja: true);  // producto dado de baja
        Assert.Equal(new[] { "004", "005" }, await new GeneradorTestable(db).ReservarAsync(2));
    }

    [Fact]
    public async Task VerificaTambienMasAllaDelPrimerBloque()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        for (var i = 1; i <= 120; i++) await ExistenteAsync(db, GeneradorCodigoBarrasService.Formatear(i), null);
        Assert.Equal(new[] { "121" }, await new GeneradorTestable(db).ReservarAsync(1));
        Assert.Equal("122", Contador(db));
    }

    [Fact]
    public async Task DespuesDe999_Sigue1000_YExcluyeLosEscritosEnLaOperacion()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db, proximo: "998");
        Assert.Equal(new[] { "998", "1000" }, await new GeneradorTestable(db).ReservarAsync(2, new[] { "999" }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task ContadorInvalido_ArrancaEn001(string valor)
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db, proximo: valor);
        Assert.Equal(new[] { "001" }, await new GeneradorTestable(db).ReservarAsync(1));
    }

    [Fact]
    public async Task SinContadorOSinTransaccion_Falla()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db, proximo: null);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => new GeneradorTestable(db).ReservarAsync(1));
        Assert.Contains("codigo_barras_automatico.sql", ex.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new GeneradorTestable(db, transaccion: false).ReservarAsync(1));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("", false)]
    public async Task Parametro_Habilita(string valor, bool esperado)
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db, valor);
        Assert.Equal(esperado, await new GeneradorTestable(db).HabilitadoAsync());
    }

    // ── Alta de producto simple ─────────────────────────────────────────────
    [Fact]
    public async Task Alta_SinCodigos_AsignaElMismoABarrasYProveedor()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        var dto = Dto();
        await Productos(db).CrearAsync(dto);
        var p = await db.Productos.SingleAsync(x => x.Id == dto.Id);
        Assert.Equal("001", p.CodBarras); Assert.Equal("001", p.CodProveedor);

        var otro = Dto("  ", "");                      // solo espacios también cuenta como vacío
        await Productos(db).CrearAsync(otro);
        Assert.Equal("002", (await db.Productos.SingleAsync(x => x.Id == otro.Id)).CodBarras);
    }

    [Theory]
    [InlineData("7790000000001", null)]   // trae código de fábrica
    [InlineData(null, "ABC-1")]
    [InlineData("7790000000001", "ABC-1")]
    public async Task Alta_ConAlgunCodigoCargado_NoSeModifica(string? barras, string? prov)
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        var dto = Dto(barras, prov);
        await Productos(db).CrearAsync(dto);
        var p = await db.Productos.SingleAsync(x => x.Id == dto.Id);
        Assert.Equal(barras, p.CodBarras); Assert.Equal(prov, p.CodProveedor);
        Assert.Equal("1", Contador(db));
    }

    [Fact]
    public async Task Alta_ConParametroApagadoOInexistente_ComportamientoActual()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db, habilitado: "0");
        var dto = Dto();
        await Productos(db).CrearAsync(dto);
        var p = await db.Productos.SingleAsync(x => x.Id == dto.Id);
        Assert.Null(p.CodBarras); Assert.Null(p.CodProveedor); Assert.Equal("1", Contador(db));

        await using var sinParametro = Entorno.NuevaBd();
        var d2 = Dto();
        await Productos(sinParametro).CrearAsync(d2);
        Assert.Null((await sinParametro.Productos.SingleAsync(x => x.Id == d2.Id)).CodBarras);
    }

    [Fact]
    public async Task Edicion_NoGeneraCodigos()
    {
        await using var db = Entorno.NuevaBd();
        var dto = Dto();
        await Productos(db).CrearAsync(dto);           // alta sin parámetro
        await ParametrosAsync(db);
        await Productos(db).ActualizarAsync(dto);
        var p = await db.Productos.SingleAsync(x => x.Id == dto.Id);
        Assert.True(string.IsNullOrEmpty(p.CodBarras)); Assert.Equal("1", Contador(db));
    }

    // ── Variantes ───────────────────────────────────────────────────────────
    [Fact]
    public async Task Variantes_CadaNuevaSinCodigoRecibeUnoUnico_LaFichaNo_YLasExistentesNoCambian()
    {
        await using var db = Entorno.NuevaBd();
        await ParametrosAsync(db);
        db.VariantesColores.AddRange(
            new VarianteColor { Id = 1, Nombre = "ROJO", Activa = true }, new VarianteColor { Id = 2, Nombre = "AZUL", Activa = true },
            new VarianteColor { Id = 3, Nombre = "NEGRO", Activa = true }, new VarianteColor { Id = 4, Nombre = "VERDE", Activa = true });
        await db.SaveChangesAsync();
        await ExistenteAsync(db, "002", null);         // ocupado

        var productos = Productos(db);
        var admin = new VarianteAdminService(db, productos, new VariantesFalso(true),
            NullLogger<VarianteAdminService>.Instance, new GeneradorTestable(db));

        var ficha = Dto(); ficha.Descripcion = "REMERA";
        var req = new GuardarVariantesRequest
        {
            UsaColores = true, PreciosIguales = true,
            Variantes = new()
            {
                new VarianteFilaDto { ColorId = 1, Habilitada = true },
                new VarianteFilaDto { ColorId = 2, Habilitada = false },                            // deshabilitada: también recibe
                new VarianteFilaDto { ColorId = 3, Habilitada = true, CodBarras = "001" },          // código escrito a mano
            }
        };
        var rv = await admin.GuardarFichaAsync(ficha, req);
        Assert.True(rv.Ok, string.Join(" | ", rv.Errores));
        Assert.Equal(2, rv.CodigosGenerados);

        var principal = await db.Productos.SingleAsync(x => x.Id == rv.ProductoId);
        Assert.True(string.IsNullOrEmpty(principal.CodBarras)); Assert.True(string.IsNullOrEmpty(principal.CodProveedor));

        async Task<Producto> VarianteAsync(int color)
        {
            var id = (await db.ProductosVariantes.SingleAsync(v => v.FkPrincipal == rv.ProductoId && v.FkColor == color)).FkProducto;
            return await db.Productos.SingleAsync(x => x.Id == id);
        }
        var roja = await VarianteAsync(1); var azul = await VarianteAsync(2); var negra = await VarianteAsync(3);
        Assert.Equal("003", roja.CodBarras); Assert.Equal("003", roja.CodProveedor);   // 001 escrito en la grilla, 002 ocupado
        Assert.Equal("004", azul.CodBarras); Assert.Equal("004", azul.CodProveedor);
        Assert.Equal("001", negra.CodBarras); Assert.Null(negra.CodProveedor);         // lo cargado se respeta

        // Se agrega una variante a la ficha existente: solo la nueva recibe código
        ficha.Id = rv.ProductoId;
        var req2 = new GuardarVariantesRequest
        {
            UsaColores = true, PreciosIguales = true,
            Variantes = new()
            {
                new VarianteFilaDto { ProductoId = roja.Id, ColorId = 1, Habilitada = true, CodBarras = "003", CodProveedor = "003" },
                new VarianteFilaDto { ColorId = 4, Habilitada = true },
            }
        };
        var rv2 = await admin.GuardarFichaAsync(ficha, req2);
        Assert.True(rv2.Ok, string.Join(" | ", rv2.Errores));
        Assert.Equal(1, rv2.CodigosGenerados);
        Assert.Equal("005", (await VarianteAsync(4)).CodBarras);
        Assert.Equal("003", (await VarianteAsync(1)).CodBarras);
        Assert.Equal("004", (await VarianteAsync(2)).CodBarras);

        var generados = await db.Productos.Where(x => x.CodBarras != null && x.CodBarras != "").Select(x => x.CodBarras).ToListAsync();
        Assert.Equal(generados.Count, generados.Distinct().Count());                    // sin duplicados
    }
}
