using Domain.DTO;
using Domain.Promociones;
using Xunit;

namespace Promociones.Tests;

/// <summary>Avisos de proximidad NxM: el producto está a UNA unidad de completar un grupo. Solo informativos.</summary>
public class ProximidadTests
{
    private static readonly DateTime Ahora = new(2026, 11, 15, 12, 0, 0);
    private const decimal P = 10000m;

    private static ReglaPromo Porc(int id, decimal pct, params int[] prods) => new()
    {
        Id = id, Nombre = $"P{pct}", Tipo = TipoPromo.Porcentaje, Porcentaje = pct,
        Desde = Ahora.AddDays(-1), Hasta = Ahora.AddDays(1), ProductoIds = prods.ToHashSet()
    };

    private static ReglaPromo Nxm(int id, int lleva, int paga, params int[] prods) => new()
    {
        Id = id, Nombre = $"{lleva}x{paga}", Tipo = TipoPromo.NxM, Lleva = lleva, Paga = paga,
        Desde = Ahora.AddDays(-1), Hasta = Ahora.AddDays(1), ProductoIds = prods.ToHashSet()
    };

    private static LineaPromoEntrada L(int clave, int prod, decimal cant, decimal precio = P)
        => new() { Clave = clave, ProductoId = prod, Cantidad = cant, PrecioSinIva = precio };

    private static List<LineaPromoProxima> Prox(IEnumerable<LineaPromoEntrada> l, params ReglaPromo[] r)
        => MotorPromociones.Proximas(l, r, Ahora);

    // ── Falta exactamente 1 unidad: se avisa ────────────────────────────────
    [Theory]
    [InlineData(2, 1, 1)]   // 2x1 con 1  → ¡agregá 1 y llevá 2 pagando 1!
    [InlineData(3, 2, 2)]   // 3x2 con 2
    [InlineData(4, 2, 3)]   // 4x2 con 3
    [InlineData(4, 3, 3)]   // 4x3 con 3
    [InlineData(3, 2, 5)]   // 3x2 con 5: falta 1 para el SIGUIENTE grupo
    [InlineData(3, 2, 8)]
    [InlineData(2, 1, 3)]   // 2x1 con 3: ya tiene un grupo y falta 1 para el próximo
    public void FaltaUnaUnidad_Avisa(int lleva, int paga, decimal cant)
    {
        var x = Assert.Single(Prox(new[] { L(4, 1, cant) }, Nxm(7, lleva, paga, 1)));
        Assert.Equal(7, x.ReglaId); Assert.Equal(lleva, x.Lleva); Assert.Equal(paga, x.Paga);
        Assert.Equal(cant, x.CantidadActual); Assert.Equal(4, x.Clave); Assert.Equal(1, x.ProductoId);
    }

    // ── Falta más de una, o ya se alcanzó el beneficio: no se avisa ──────────
    [Theory]
    [InlineData(3, 2, 1)]   // falta 2
    [InlineData(3, 2, 4)]   // falta 2 para el siguiente grupo (decisión: solo "falta exactamente 1")
    [InlineData(4, 2, 1)]
    [InlineData(4, 2, 2)]
    [InlineData(3, 2, 3)]   // promoción alcanzada
    [InlineData(3, 2, 6)]   // dos grupos completos
    [InlineData(2, 1, 2)]
    [InlineData(2, 1, 4)]
    [InlineData(4, 2, 4)]
    public void NoFaltaUnaOYaAlcanzada_NoAvisa(int lleva, int paga, decimal cant)
        => Assert.Empty(Prox(new[] { L(0, 1, cant) }, Nxm(1, lleva, paga, 1)));

    // ── Sin promoción aplicable ─────────────────────────────────────────────
    [Fact]
    public void SinPromociones_ProductoNoAsociado_SoloPorcentaje_NoAvisa()
    {
        Assert.Empty(Prox(new[] { L(0, 1, 1) }));
        Assert.Empty(Prox(new[] { L(0, 99, 1) }, Nxm(1, 2, 1, 1)));          // producto que no participa
        Assert.Empty(Prox(new[] { L(0, 1, 1) }, Porc(1, 20m, 1)));           // solo porcentaje
    }

    [Fact]
    public void FueraDeVigencia_Inactiva_NoAvisa()
    {
        var vencida = Nxm(1, 2, 1, 1); vencida.Hasta = Ahora.AddMinutes(-1);
        var futura  = Nxm(2, 2, 1, 1); futura.Desde = Ahora.AddMinutes(1);
        var inact   = Nxm(3, 2, 1, 1); inact.Activa = false;
        Assert.Empty(Prox(new[] { L(0, 1, 1) }, vencida, futura, inact));
    }

    [Fact]
    public void Vigencia_ExtremoInclusivo_Avisa()
    {
        var r = Nxm(1, 2, 1, 1); r.Desde = Ahora; r.Hasta = Ahora;
        Assert.Single(Prox(new[] { L(0, 1, 1) }, r));
    }

    // ── Varias promociones ──────────────────────────────────────────────────
    [Fact]
    public void VariasNxm_SeAvisaLaMasConvenienteAlAgregarLaUnidad()
    {
        // Con 3 unidades: 3x2 ya aplica. Al agregar 1: 4x2 (paga 2) supera a 3x2 (paga 3) → se anuncia 4x2.
        var x = Assert.Single(Prox(new[] { L(0, 1, 3) }, Nxm(1, 4, 2, 1), Nxm(2, 3, 2, 1)));
        Assert.Equal(1, x.ReglaId); Assert.Equal(4, x.Lleva); Assert.Equal(2, x.Paga);
    }

    [Fact]
    public void VariasNxm_SiNoMejoraElBeneficioActual_NoAvisa()
    {
        // 2x1 con 2 unidades ya bonifica 1; con 3 sigue bonificando 1 (3x2 empata y gana 2x1 por menor Id)
        Assert.Empty(Prox(new[] { L(0, 1, 2) }, Nxm(1, 2, 1, 1), Nxm(2, 3, 2, 1)));
    }

    [Fact]
    public void Nxm_ContraPorcentaje_SoloAvisaSiLaNxmGanariaConLaUnidadExtra()
    {
        // 3x2 + 20 %: con 2 unidades gana el 20 %; con 3 gana 3x2 (paga 2 < 2,4) → se avisa
        var x = Assert.Single(Prox(new[] { L(0, 1, 2) }, Nxm(1, 3, 2, 1), Porc(2, 20m, 1)));
        Assert.Equal(1, x.ReglaId);
        // 3x2 + 40 %: con 3 unidades seguiría ganando el 40 % (1,8 < 2) → NO se anuncia algo que no se aplicaría
        Assert.Empty(Prox(new[] { L(0, 1, 2) }, Nxm(1, 3, 2, 1), Porc(2, 40m, 1)));
    }

    // ── Cantidades y líneas ─────────────────────────────────────────────────
    [Fact]
    public void ProductoRepetido_SumaLasLineas_YAvisaEnLaPrimera()
    {
        var x = Assert.Single(Prox(new[] { L(5, 1, 1), L(9, 1, 1) }, Nxm(1, 3, 2, 1)));   // 1 + 1 = 2 → falta 1
        Assert.Equal(5, x.Clave); Assert.Equal(2m, x.CantidadActual);
        Assert.Empty(Prox(new[] { L(0, 1, 2), L(1, 1, 2) }, Nxm(1, 3, 2, 1)));          // 2 + 2 = 4 → falta 2
    }

    [Fact]
    public void CantidadDecimal_NoAvisa()
        => Assert.Empty(Prox(new[] { L(0, 1, 1.5m) }, Nxm(1, 2, 1, 1)));

    [Fact]
    public void VariosProductos_CadaUnoConSuPropiaCantidad()
    {
        var r = Prox(new[] { L(0, 1, 2), L(1, 2, 3), L(2, 3, 1) }, Nxm(1, 3, 2, 1), Nxm(2, 4, 3, 2));
        Assert.Equal(2, r.Count);
        Assert.Contains(r, x => x.ProductoId == 1 && x.Lleva == 3);
        Assert.Contains(r, x => x.ProductoId == 2 && x.Lleva == 4);
        Assert.DoesNotContain(r, x => x.ProductoId == 3);
    }

    [Fact]
    public void LineasSinPrecioOCantidad_NoAvisan()
    {
        Assert.Empty(Prox(new[] { L(0, 1, 1, 0m) }, Nxm(1, 2, 1, 1)));
        Assert.Empty(Prox(new[] { L(0, 1, 0m) }, Nxm(1, 2, 1, 1)));
    }

    [Fact]
    public void ModificarCantidad_ElAvisoAparecePasaYDesaparece()
    {
        var r = Nxm(1, 3, 2, 1);
        Assert.Empty(Prox(new[] { L(0, 1, 1) }, r));        // falta 2
        Assert.Single(Prox(new[] { L(0, 1, 2) }, r));       // falta 1 → aviso
        Assert.Empty(Prox(new[] { L(0, 1, 3) }, r));        // alcanzada
        Assert.Empty(Prox(new[] { L(0, 1, 0) }, r));        // se eliminó la cantidad
    }

    [Fact]
    public void Proximas_NoAlteraElResultadoDeEvaluar()
    {
        var reglas = new[] { Nxm(1, 3, 2, 1) };
        var lineas = new[] { L(0, 1, 5) };
        var antes = MotorPromociones.Evaluar(lineas, reglas, Ahora);
        _ = MotorPromociones.Proximas(lineas, reglas, Ahora);
        var despues = MotorPromociones.Evaluar(lineas, reglas, Ahora);
        Assert.Equal(antes.Single().CantBonificada, despues.Single().CantBonificada);
        Assert.Equal(1m, despues.Single().CantBonificada);   // 5 unidades en 3x2: un grupo, sin cambios por el aviso
    }
}

public class ProximidadServicioTests
{
    [Fact]
    public async Task Evaluar_DevuelveAvisoConTextoComercial()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "SHAMPOO", 10000);
        await Entorno.ReglaAsync(db, "3X2 SHAMPOO", "NXM", null, 3, 2, new[] { p.Id });

        var ev = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 4, FkProducto = p.Id, Cantidad = 2, PrecioSinIva = 10000 } });
        var x = Assert.Single(ev.Proximas);
        Assert.Equal(4, x.Clave); Assert.Equal("SHAMPOO", x.Descripcion);
        Assert.Contains("3X2 SHAMPOO", x.Texto);
        Assert.Contains("Agregando 1 unidad más, llevás 3 y pagás 2", x.Texto);
        Assert.Equal("¡+1 y llevá 3 pagando 2!", x.TextoCorto);
        Assert.Empty(ev.Lineas);                      // todavía no hay beneficio aplicado: no se toca ningún importe
        Assert.Empty(ev.Resumen);
    }

    [Fact]
    public async Task Evaluar_ConBeneficioAlcanzado_NoHayAviso_YConUnGrupoMasUnoSi()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var p = await Entorno.ProductoAsync(db, "S", 100);
        await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, new[] { p.Id });
        var s = Entorno.Servicio(db);
        PromoEvalLineaDto L(decimal c) => new() { Clave = 0, FkProducto = p.Id, Cantidad = c, PrecioSinIva = 100 };

        var tres = await s.EvaluarAsync(new[] { L(3) });
        Assert.Empty(tres.Proximas); Assert.Single(tres.Lineas);                       // alcanzada

        var cinco = await s.EvaluarAsync(new[] { L(5) });
        Assert.Single(cinco.Proximas); Assert.Single(cinco.Lineas);                    // un grupo aplicado y falta 1 para el siguiente

        var cuatro = await s.EvaluarAsync(new[] { L(4) });
        Assert.Empty(cuatro.Proximas);                                                  // falta 2
    }

    [Fact]
    public async Task Evaluar_SinMotor_FueraDeVigencia_Fraccionado_NoAvisa()
    {
        await using var db = Entorno.NuevaBd();                                         // motor apagado
        var p = await Entorno.ProductoAsync(db, "A", 100);
        await Entorno.ReglaAsync(db, "2X1", "NXM", null, 2, 1, new[] { p.Id });
        var apagado = await Entorno.Servicio(db).EvaluarAsync(new[] { new PromoEvalLineaDto { Clave = 0, FkProducto = p.Id, Cantidad = 1, PrecioSinIva = 100 } });
        Assert.Empty(apagado.Proximas);

        await using var db2 = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db2, "1");
        var q = await Entorno.ProductoAsync(db2, "B", 100);
        await Entorno.ReglaAsync(db2, "VENCIDA", "NXM", null, 2, 1, new[] { q.Id }, TimeSpan.FromDays(-2), TimeSpan.FromMinutes(-1));
        var f = await Entorno.ProductoAsync(db2, "QUESO", 100, fraccionado: true);
        await Entorno.ReglaAsync(db2, "2X1 QUESO", "NXM", null, 2, 1, new[] { f.Id });
        var ev = await Entorno.Servicio(db2).EvaluarAsync(new[]
        {
            new PromoEvalLineaDto { Clave = 0, FkProducto = q.Id, Cantidad = 1, PrecioSinIva = 100 },
            new PromoEvalLineaDto { Clave = 1, FkProducto = f.Id, Cantidad = 1, PrecioSinIva = 100 }
        });
        Assert.Empty(ev.Proximas);
    }
}
