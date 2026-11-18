using Domain.Promociones;
using Xunit;

namespace Promociones.Tests;

/// <summary>Pruebas del motor puro de promociones (cantidad NxM y porcentaje). Precio de referencia: $10.000.</summary>
public class MotorPromocionesTests
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

    private static List<LineaPromoResultado> Ev(IEnumerable<LineaPromoEntrada> l, params ReglaPromo[] r)
        => MotorPromociones.Evaluar(l, r, Ahora);

    // ── NxM ─────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(2, 2, 1, 1, 10000)]   // 2x1 con dos unidades: 1 bonificada, ahorro 10.000
    [InlineData(4, 2, 1, 2, 20000)]   // 2x1 con cuatro: 2 bonificadas
    [InlineData(3, 3, 2, 1, 10000)]   // 3x2 con tres
    [InlineData(6, 3, 2, 2, 20000)]   // 3x2 con seis: dos grupos
    [InlineData(5, 3, 2, 1, 10000)]   // 3x2 con cinco: un grupo, 2 unidades conservan precio
    [InlineData(4, 4, 2, 2, 20000)]   // 4x2
    [InlineData(4, 4, 3, 1, 10000)]   // 4x3
    [InlineData(9, 4, 2, 4, 40000)]   // 4x2 con 9: dos grupos (8) + 1 suelta
    public void Nxm_BonificaPorGruposCompletos(decimal cant, int lleva, int paga, decimal bonif, decimal ahorro)
    {
        var r = Ev(new[] { L(0, 1, cant) }, Nxm(1, lleva, paga, 1));
        var x = Assert.Single(r);
        Assert.Equal(bonif, x.CantBonificada);
        Assert.Equal(ahorro, x.Ahorro);
        Assert.Equal(Math.Round(bonif / cant * 100m, 8), x.DescuentoPct);
    }

    [Theory]
    [InlineData(1, 2, 1)]   // 2x1 con una unidad: no completa grupo
    [InlineData(2, 3, 2)]   // 3x2 con dos unidades
    [InlineData(3, 4, 2)]
    public void Nxm_CantidadInferiorAlGrupo_NoAplica(decimal cant, int lleva, int paga)
        => Assert.Empty(Ev(new[] { L(0, 1, cant) }, Nxm(1, lleva, paga, 1)));

    [Fact]
    public void Nxm_ReglaInvalida_Ignorada()
    {
        Assert.Empty(Ev(new[] { L(0, 1, 6) }, Nxm(1, 2, 2, 1)));   // paga == lleva
        Assert.Empty(Ev(new[] { L(0, 1, 6) }, Nxm(2, 2, 0, 1)));   // paga < 1
    }

    // ── Porcentaje ──────────────────────────────────────────────────────────
    [Fact]
    public void Porcentaje_UnProducto()
    {
        var x = Assert.Single(Ev(new[] { L(0, 1, 3) }, Porc(1, 20m, 1)));
        Assert.Equal(20m, x.DescuentoPct);
        Assert.Equal(0m, x.CantBonificada);
        Assert.Equal(6000m, x.Ahorro);
    }

    [Fact]
    public void Porcentaje_VariosProductos_SoloLosAsociados()
    {
        var r = Ev(new[] { L(0, 1, 1, 10000m), L(1, 2, 1, 15000m), L(2, 3, 1, 8000m) }, Porc(1, 15m, 1, 2));
        Assert.Equal(2, r.Count);
        Assert.DoesNotContain(r, x => x.ProductoId == 3);   // producto que no participa: sin cambios
        Assert.Equal(2250m, r.Single(x => x.ProductoId == 2).Ahorro);
    }

    // ── Superposición ───────────────────────────────────────────────────────
    [Theory]
    // cant, ganadora esperada (A=10 %, B=20 %, C=2x1) con precio $10.000
    [InlineData(1, 2)]   // 1 unidad: B (2x1 no aplica)
    [InlineData(2, 3)]   // 2 unidades: 2x1 (10.000) < 20 % (16.000)
    [InlineData(3, 3)]   // 3: 2x1 → 20.000 < 24.000
    [InlineData(5, 3)]   // 5: 2x1 → 30.000 < 40.000
    public void Superpuestas_GanaLaMasConvenienteSegunCantidad(decimal cant, int ganadora)
    {
        var x = Assert.Single(Ev(new[] { L(0, 1, cant) }, Porc(1, 10m, 1), Porc(2, 20m, 1), Nxm(3, 2, 1, 1)));
        Assert.Equal(ganadora, x.ReglaId);
    }

    [Fact]
    public void Superpuestas_PorcentajeMayorVenceA3x2_ConCantidadElevada()
    {
        // 3x2 paga 2/3 (33,3 % de ahorro) frente a 40 %: gana el 40 %
        var x = Assert.Single(Ev(new[] { L(0, 1, 3) }, Nxm(1, 3, 2, 1), Porc(2, 40m, 1)));
        Assert.Equal(2, x.ReglaId);
        // con 7 unidades 3x2 paga 5 (28,6 %) -> sigue ganando el 40 %
        x = Assert.Single(Ev(new[] { L(0, 1, 7) }, Nxm(1, 3, 2, 1), Porc(2, 40m, 1)));
        Assert.Equal(2, x.ReglaId);
    }

    [Fact]
    public void Superpuestas_DosPorcentajes_NoSeAcumulan()
    {
        var x = Assert.Single(Ev(new[] { L(0, 1, 1) }, Porc(1, 10m, 1), Porc(2, 15m, 1)));
        Assert.Equal(2, x.ReglaId);
        Assert.Equal(15m, x.DescuentoPct);   // 15 %, no 25 % ni 23,5 %
    }

    [Fact]
    public void Superpuestas_DosNxm_SoloUna()
    {
        // 2x1 con 4 unidades paga 2; 3x2 con 4 unidades paga 3 -> gana 2x1
        var x = Assert.Single(Ev(new[] { L(0, 1, 4) }, Nxm(1, 3, 2, 1), Nxm(2, 2, 1, 1)));
        Assert.Equal(2, x.ReglaId);
        Assert.Equal(2m, x.CantBonificada);
    }

    [Fact]
    public void Desempate_GanaLaReglaDeMenorId_AunqueVengaDespues()
    {
        // 50 % y 2x1 con 2 unidades dan exactamente el mismo importe (10.000)
        var r = Ev(new[] { L(0, 1, 2) }, Nxm(7, 2, 1, 1), Porc(3, 50m, 1));
        Assert.Equal(3, Assert.Single(r).ReglaId);
        r = Ev(new[] { L(0, 1, 2) }, Porc(9, 50m, 1), Nxm(4, 2, 1, 1));
        Assert.Equal(4, Assert.Single(r).ReglaId);
    }

    // ── Vigencia ────────────────────────────────────────────────────────────
    [Fact]
    public void Vigencia_FuturaVencidaInactiva_NoAplican()
    {
        var futura  = Porc(1, 50m, 1); futura.Desde = Ahora.AddMinutes(1);  futura.Hasta = Ahora.AddDays(1);
        var vencida = Porc(2, 50m, 1); vencida.Desde = Ahora.AddDays(-2);   vencida.Hasta = Ahora.AddMinutes(-1);
        var inact   = Porc(3, 50m, 1); inact.Activa = false;
        Assert.Empty(Ev(new[] { L(0, 1, 1) }, futura, vencida, inact));
    }

    [Fact]
    public void Vigencia_ExtremosInclusivos_YOtraVigenteSeUsa()
    {
        var justo = Porc(1, 10m, 1); justo.Desde = Ahora; justo.Hasta = Ahora;
        Assert.Single(Ev(new[] { L(0, 1, 1) }, justo));
        var vencida = Porc(2, 90m, 1); vencida.Hasta = Ahora.AddSeconds(-1);
        Assert.Equal(1, Assert.Single(Ev(new[] { L(0, 1, 1) }, vencida, justo)).ReglaId);
    }

    // ── Cantidades / líneas ─────────────────────────────────────────────────
    [Fact]
    public void Comparacion_UsaCantidadTotalDelProducto_AunConVariasLineas()
    {
        // dos líneas de 1 unidad del mismo producto suman 2: 2x1 aplica y bonifica 1 unidad (en la primera línea)
        var r = Ev(new[] { L(0, 1, 1), L(1, 1, 1) }, Nxm(1, 2, 1, 1));
        var x = Assert.Single(r);
        Assert.Equal(0, x.Clave);
        Assert.Equal(100m, x.DescuentoPct);
        Assert.Equal(1m, x.CantBonificada);
    }

    [Fact]
    public void Nxm_ReparteBonificadasEntreLineas_EnOrden()
    {
        // 2 + 3 unidades con 3x2: 5 unidades → 1 grupo → 1 bonificada: va a la primera línea (2 unidades → 50 %)
        var r = Ev(new[] { L(0, 1, 2), L(1, 1, 3) }, Nxm(1, 3, 2, 1));
        var x = Assert.Single(r);
        Assert.Equal(0, x.Clave);
        Assert.Equal(1m, x.CantBonificada);
        Assert.Equal(50m, x.DescuentoPct);
    }

    [Fact]
    public void SinReglas_PrecioCeroOCantidadCero_SinPromocion()
    {
        Assert.Empty(Ev(new[] { L(0, 1, 5) }));
        Assert.Empty(Ev(new[] { L(0, 1, 5, 0m) }, Porc(1, 10m, 1)));
        Assert.Empty(Ev(new[] { L(0, 1, 0m) }, Porc(1, 10m, 1)));
    }

    [Fact]
    public void ProductoNoAsociado_NoSeAltera()
        => Assert.Empty(Ev(new[] { L(0, 99, 10) }, Porc(1, 50m, 1), Nxm(2, 2, 1, 1)));

    [Fact]
    public void Cantidad_Decimal_NoCompletaGrupo()
    {
        // 2,5 unidades con 2x1: 1 grupo completo (floor(2,5/2)=1) → 1 bonificada
        var x = Assert.Single(Ev(new[] { L(0, 1, 2.5m) }, Nxm(1, 2, 1, 1)));
        Assert.Equal(1m, x.CantBonificada);
    }

    [Fact]
    public void ImportesFinales_SonLosEsperados_ParaEjemploDelEnunciado()
    {
        // Producto A $10.000 x1, 20 %  → $8.000 ; 4 unidades con 2x1 → paga $20.000
        var a = Assert.Single(Ev(new[] { L(0, 1, 1) }, Porc(1, 20m, 1)));
        Assert.Equal(8000m, P - a.Ahorro);
        var b = Assert.Single(Ev(new[] { L(0, 1, 4) }, Nxm(1, 2, 1, 1)));
        Assert.Equal(20000m, 4 * P - b.Ahorro);
    }
}
