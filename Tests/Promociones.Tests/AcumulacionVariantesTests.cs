using Domain.DTO;
using Domain.Promociones;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Promociones.Tests;

/// <summary>NxM con "Acumular variantes de una misma ficha" (agrupaVariantes): motor.</summary>
public class AcumulacionVariantesMotorTests
{
    private static readonly DateTime Ahora = new(2026, 11, 15, 12, 0, 0);
    private const decimal P = 100m;

    private static ReglaPromo Nxm(int id, int lleva, int paga, bool agrupa, params int[] prods) => new()
    {
        Id = id, Nombre = $"{lleva}x{paga}", Tipo = TipoPromo.NxM, Lleva = lleva, Paga = paga, AgrupaVariantes = agrupa,
        Desde = Ahora.AddDays(-1), Hasta = Ahora.AddDays(1), ProductoIds = prods.ToHashSet()
    };

    private static ReglaPromo Porc(int id, decimal pct, params int[] prods) => new()
    {
        Id = id, Nombre = $"{pct}%", Tipo = TipoPromo.Porcentaje, Porcentaje = pct,
        Desde = Ahora.AddDays(-1), Hasta = Ahora.AddDays(1), ProductoIds = prods.ToHashSet()
    };

    // clave, producto, cantidad, ficha
    private static LineaPromoEntrada L(int clave, int prod, decimal cant, int? ficha = null, decimal precio = P)
        => new() { Clave = clave, ProductoId = prod, Cantidad = cant, PrecioSinIva = precio, FichaId = ficha };

    private static List<LineaPromoResultado> Ev(IEnumerable<LineaPromoEntrada> l, params ReglaPromo[] r)
        => MotorPromociones.Evaluar(l, r, Ahora);

    // ── 1. Desmarcado: comportamiento actual ─────────────────────────────────
    [Fact]
    public void Desmarcado_CadaVarianteSeEvaluaPorSeparado()
    {
        // 3 variantes de una ficha, 1 unidad cada una, 3x2: sin acumular ninguna completa el grupo
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, Nxm(1, 3, 2, false, 11, 12, 13));
        Assert.Empty(r);
    }

    [Fact]
    public void Desmarcado_ConFichasOSinFichas_DaElMismoResultado()
    {
        var reglas = new[] { Nxm(1, 3, 2, false, 11, 12), Porc(2, 25m, 13) };
        var conFicha = Ev(new[] { L(0, 11, 3, 1), L(1, 12, 1, 1), L(2, 13, 2, 1) }, reglas);
        var sinFicha = Ev(new[] { L(0, 11, 3), L(1, 12, 1), L(2, 13, 2) }, reglas);
        Assert.Equal(sinFicha.Select(Huella), conFicha.Select(Huella));
    }

    private static string Huella(LineaPromoResultado x) => $"{x.Clave}|{x.ReglaId}|{x.CantBonificada}|{x.DescuentoPct}|{x.Ahorro}";

    [Fact]
    public void Equivalencia_ConReglasQueNoAcumulan_ElResultadoNoDependeDeLasFichas()
    {
        // 300 casos pseudo-aleatorios (semilla fija): sin reglas que acumulen, las fichas no deben cambiar nada.
        var rnd = new Random(12345);
        for (int caso = 0; caso < 300; caso++)
        {
            var prods = Enumerable.Range(1, 6).ToArray();
            var reglas = new List<ReglaPromo>();
            for (int i = 1; i <= rnd.Next(1, 5); i++)
            {
                var asoc = prods.Where(_ => rnd.Next(2) == 0).ToArray();
                if (asoc.Length == 0) asoc = new[] { 1 };
                reglas.Add(rnd.Next(2) == 0 ? Porc(i, rnd.Next(5, 60), asoc)
                                            : Nxm(i, rnd.Next(2, 5), 1, false, asoc));   // paga 1 < lleva: siempre válida
            }
            var lineas = new List<(int p, decimal c, int f)>();
            for (int k = 0; k < rnd.Next(1, 7); k++) lineas.Add((prods[rnd.Next(prods.Length)], rnd.Next(1, 8), rnd.Next(1, 3)));
            var con = lineas.Select((x, i) => L(i, x.p, x.c, x.f)).ToList();
            var sin = lineas.Select((x, i) => L(i, x.p, x.c)).ToList();
            Assert.Equal(Ev(sin, reglas.ToArray()).Select(Huella), Ev(con, reglas.ToArray()).Select(Huella));
            Assert.Equal(MotorPromociones.Proximas(sin, reglas, Ahora).Select(p => $"{p.Clave}|{p.ReglaId}|{p.CantidadActual}"),
                         MotorPromociones.Proximas(con, reglas, Ahora).Select(p => $"{p.Clave}|{p.ReglaId}|{p.CantidadActual}"));
        }
    }

    // ── 2. Marcado: las variantes de una misma ficha completan el grupo ─────
    [Fact]
    public void Marcado_TresVariantesDeUnaMismaFicha_CompletanUn3x2()
    {
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, Nxm(1, 3, 2, true, 11, 12, 13));
        var x = Assert.Single(r);                       // se cobra 2 de 3: una unidad bonificada
        Assert.Equal(0, x.Clave); Assert.Equal(1m, x.CantBonificada); Assert.Equal(100m, x.DescuentoPct);
        Assert.Equal(P, x.Ahorro); Assert.True(x.AgrupaVariantes);
    }

    [Fact]
    public void Marcado_DosPorDos_CompletaUnGrupo_Y_QuedaUnaUnidadFuera()
    {
        var r = Ev(new[] { L(0, 11, 2, 1), L(1, 12, 2, 1) }, Nxm(1, 3, 2, true, 11, 12));
        var x = Assert.Single(r);                       // 4 unidades: un grupo de 3 (1 bonificada) y 1 sobrante
        Assert.Equal(0, x.Clave); Assert.Equal(1m, x.CantBonificada); Assert.Equal(50m, x.DescuentoPct);   // 1 de las 2 unidades de la primera línea
        Assert.Equal(1m, r.Sum(y => y.CantBonificada));
    }

    [Fact]
    public void Marcado_CantidadesMayoresAlGrupo_VariosGruposYSobrantes()
    {
        // 3 + 2 + 2 = 7 unidades en 3x2: 2 grupos completos → 2 bonificadas; sobra 1 unidad (precio normal)
        var r = Ev(new[] { L(0, 11, 3, 1), L(1, 12, 2, 1), L(2, 13, 2, 1) }, Nxm(1, 3, 2, true, 11, 12, 13));
        Assert.Equal(2m, r.Sum(x => x.CantBonificada));
        var x0 = Assert.Single(r);                      // las 2 bonificadas caen en la primera línea (en orden de línea)
        Assert.Equal(0, x0.Clave); Assert.Equal(Math.Round(2m / 3m * 100m, 8), x0.DescuentoPct);
        Assert.Equal(2 * P, x0.Ahorro);
    }

    [Fact]
    public void Marcado_LasBonificadasSeReparten_EnOrdenDeLinea()
    {
        // 1 + 1 + 1 + 1 + 1 + 1 = 6 unidades en 3x2 → 2 bonificadas: primeras dos líneas
        var r = Ev(Enumerable.Range(0, 6).Select(i => L(i, 11 + i, 1, 1)), Nxm(1, 3, 2, true, 11, 12, 13, 14, 15, 16));
        Assert.Equal(new[] { 0, 1 }, r.Select(x => x.Clave).OrderBy(c => c).ToArray());
        Assert.All(r, x => Assert.Equal(100m, x.DescuentoPct));
    }

    // ── 3. Fichas diferentes no acumulan ────────────────────────────────────
    [Fact]
    public void FichasDistintas_NoSeAcumulanEntreSi()
    {
        var regla = Nxm(1, 3, 2, true, 11, 12, 21, 22);
        Assert.Empty(Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 21, 1, 2) }, regla));                         // 2 + 1: ninguna ficha llega a 3
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1), L(3, 21, 1, 2), L(4, 22, 1, 2), L(5, 23, 1, 2) },
                   Nxm(1, 3, 2, true, 11, 12, 13, 21, 22, 23));
        Assert.Equal(2m, r.Sum(x => x.CantBonificada));                                                            // una bonificada por ficha
        Assert.Contains(r, x => x.Clave == 0); Assert.Contains(r, x => x.Clave == 3);
    }

    // ── 4. Variantes no incluidas en la promoción no participan ─────────────
    [Fact]
    public void VariantesNoIncluidas_NoParticipanNiSuman()
    {
        // la regla incluye solo 11 y 12; la 13 (de la misma ficha) tiene 5 unidades y NO cuenta
        Assert.Empty(Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 5, 1) }, Nxm(1, 3, 2, true, 11, 12)));
        var r = Ev(new[] { L(0, 11, 2, 1), L(1, 12, 1, 1), L(2, 13, 5, 1) }, Nxm(1, 3, 2, true, 11, 12));
        var x = Assert.Single(r);
        Assert.Equal(0, x.Clave);                                      // 2 + 1 = 3: grupo completo; la variante 13 queda intacta
        Assert.DoesNotContain(r, y => y.ProductoId == 13);
    }

    [Fact]
    public void ProductoSinFicha_EnReglaQueAcumula_SeEvaluaComoSiempre()
    {
        var r = Ev(new[] { L(0, 11, 2) }, Nxm(1, 2, 1, true, 11));    // no es variante: 2x1 por producto
        var x = Assert.Single(r);
        Assert.Equal(1m, x.CantBonificada);
    }

    // ── 5. Convivencia con otras promociones ────────────────────────────────
    [Fact]
    public void PorcentajeMuchoMayor_VenceAlAcumulado_YCadaVarianteTieneSuPromo()
    {
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, Nxm(1, 3, 2, true, 11, 12, 13), Porc(2, 90m, 11, 12, 13));
        Assert.Equal(3, r.Count);
        Assert.All(r, x => { Assert.Equal(2, x.ReglaId); Assert.Equal(90m, x.DescuentoPct); });   // ahorro 270 > 100
    }

    [Fact]
    public void AcumuladoMayorAhorro_VenceAUnPorcentajeSobreUnaSolaVariante()
    {
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, Nxm(1, 3, 2, true, 11, 12, 13), Porc(2, 40m, 11));
        Assert.All(r, x => Assert.Equal(1, x.ReglaId));                // 100 (acumulado) > 40 (porcentaje en una variante)
        Assert.Single(r);
    }

    [Fact]
    public void DosReglasAcumuladas_Solapadas_GanaLaDeMayorAhorro_YDesempateMenorId()
    {
        // 3x2 sobre {11,12,13} y 2x1 sobre {11,12}, 1 unidad cada una: ambas ahorran 100 → gana la de menor Id
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, Nxm(2, 2, 1, true, 11, 12), Nxm(1, 3, 2, true, 11, 12, 13));
        Assert.All(r, x => Assert.Equal(1, x.ReglaId));
        Assert.Single(r);
    }

    [Fact]
    public void DosReglasAcumuladas_Disjuntas_AplicanCadaUnaALoSuyo()
    {
        var reglas = new[] { Nxm(1, 3, 2, true, 11, 12, 13), Nxm(2, 2, 1, true, 14, 15) };
        var r = Ev(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1), L(3, 14, 1, 1), L(4, 15, 1, 1) }, reglas);
        Assert.Equal(2, r.Count);
        Assert.Contains(r, x => x.ReglaId == 1 && x.Clave == 0);
        Assert.Contains(r, x => x.ReglaId == 2 && x.Clave == 3);
    }

    [Fact]
    public void PrecioDistintoPorVariante_AhorroSegunLaUnidadBonificada()
    {
        var r = Ev(new[] { L(0, 11, 1, 1, 100m), L(1, 12, 1, 1, 200m), L(2, 13, 1, 1, 300m) }, Nxm(1, 3, 2, true, 11, 12, 13));
        var x = Assert.Single(r);
        Assert.Equal(0, x.Clave); Assert.Equal(100m, x.Ahorro);        // se bonifica la primera línea (lógica actual de orden)
    }

    // ── Avisos de proximidad ────────────────────────────────────────────────
    [Fact]
    public void Proximas_ConAcumulacion_SumaLaFicha_YAvisaEnLaPrimeraLinea()
    {
        var regla = Nxm(1, 3, 2, true, 11, 12, 13);
        var x = Assert.Single(MotorPromociones.Proximas(new[] { L(5, 11, 1, 1), L(6, 12, 1, 1) }, new[] { regla }, Ahora));
        Assert.Equal(5, x.Clave); Assert.Equal(2m, x.CantidadActual); Assert.True(x.Agrupa);
        Assert.Empty(MotorPromociones.Proximas(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, new[] { regla }, Ahora));   // alcanzada
        Assert.Empty(MotorPromociones.Proximas(new[] { L(0, 11, 2, 1), L(1, 12, 2, 1) }, new[] { regla }, Ahora));                    // 4: falta 2
        Assert.Single(MotorPromociones.Proximas(new[] { L(0, 11, 3, 1), L(1, 12, 2, 1) }, new[] { regla }, Ahora));                   // 5: falta 1
    }

    [Fact]
    public void Proximas_SinAcumulacion_OConFichasDistintas_NoAvisa()
    {
        Assert.Empty(MotorPromociones.Proximas(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1) }, new[] { Nxm(1, 3, 2, false, 11, 12) }, Ahora));
        var regla = Nxm(1, 3, 2, true, 11, 12, 21);
        Assert.Empty(MotorPromociones.Proximas(new[] { L(0, 11, 1, 1), L(1, 21, 1, 2) }, new[] { regla }, Ahora));                    // fichas distintas
        var x = Assert.Single(MotorPromociones.Proximas(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 21, 1, 2) }, new[] { regla }, Ahora));
        Assert.Equal(0, x.Clave);                                                                                                       // solo la ficha 1 (2 unidades)
    }

    [Fact]
    public void Proximas_VarianteNoIncluida_NoCuenta()
    {
        var regla = Nxm(1, 3, 2, true, 11, 12);
        var x = Assert.Single(MotorPromociones.Proximas(new[] { L(0, 11, 1, 1), L(1, 12, 1, 1), L(2, 13, 1, 1) }, new[] { regla }, Ahora));
        Assert.Equal(2m, x.CantidadActual);                                                                                             // la 13 no suma
    }
}

/// <summary>Acumulación de variantes: servicio (ficha ← productosVariantes), ABM y guardado real.</summary>
public class AcumulacionVariantesServicioTests
{
    private static PromoEvalLineaDto E(int clave, int prod, decimal cant, decimal precio = 10000m)
        => new() { Clave = clave, FkProducto = prod, Cantidad = cant, PrecioSinIva = precio };

    [Fact]
    public async Task Evaluar_ConVariantesHabilitadas_AcumulaLasDeUnaMismaFicha()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        await Entorno.ReglaAsync(db, "3X2 REMERAS", "NXM", null, 3, 2, v, agrupa: true);
        var ev = await Entorno.Servicio(db, variantes: true).EvaluarAsync(new[] { E(0, v[0], 1), E(1, v[1], 1), E(2, v[2], 1) });

        var l = Assert.Single(ev.Lineas);
        Assert.Equal(0, l.Clave); Assert.Equal(1m, l.CantBonificada); Assert.Equal(100m, l.DescuentoPct);
        Assert.Contains("acumulando variantes", Assert.Single(ev.Resumen).Texto);
    }

    [Fact]
    public async Task Evaluar_ReglaSinAcumular_ONoSeTrabajaConVariantes_NoAcumula()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        await Entorno.ReglaAsync(db, "NO ACUMULA", "NXM", null, 3, 2, v, agrupa: false);
        var ev = await Entorno.Servicio(db, variantes: true).EvaluarAsync(new[] { E(0, v[0], 1), E(1, v[1], 1), E(2, v[2], 1) });
        Assert.Empty(ev.Lineas);                                       // comportamiento actual: cada variante por separado

        await using var db2 = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db2, "1");
        var w = await Entorno.VariantesAsync(db2, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        await Entorno.ReglaAsync(db2, "ACUMULA", "NXM", null, 3, 2, w, agrupa: true);
        var sinVariantes = await Entorno.Servicio(db2, variantes: false).EvaluarAsync(new[] { E(0, w[0], 1), E(1, w[1], 1), E(2, w[2], 1) });
        Assert.Empty(sinVariantes.Lineas);                             // sin el módulo de variantes no se consulta productosVariantes
    }

    [Fact]
    public async Task Evaluar_FichasDistintas_YVariantesNoIncluidas()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var a = await Entorno.VariantesAsync(db, "REMERA", 10000, "R-ROJA", "R-AZUL");
        var b = await Entorno.VariantesAsync(db, "PANTALON", 10000, "P-NEGRO");
        await Entorno.ReglaAsync(db, "3X2 MIXTA", "NXM", null, 3, 2, a.Concat(b).ToArray(), agrupa: true);
        var s = Entorno.Servicio(db, variantes: true);
        // 2 de la remera + 1 del pantalón: fichas distintas, no suman 3
        Assert.Empty((await s.EvaluarAsync(new[] { E(0, a[0], 1), E(1, a[1], 1), E(2, b[0], 1) })).Lineas);
        // aviso: la remera suma 2 → falta 1 (el pantalón es otra ficha)
        var ev = await s.EvaluarAsync(new[] { E(0, a[0], 1), E(1, a[1], 1), E(2, b[0], 1) });
        var p = Assert.Single(ev.Proximas);
        Assert.Contains("cualquier variante de esta ficha", p.Texto);
    }

    [Fact]
    public async Task Abm_PersisteLaOpcion_SoloParaNxm()
    {
        await using var db = Entorno.NuevaBd();
        var v = await Entorno.VariantesAsync(db, "REMERA", 100, "ROJA", "AZUL");
        var s = Entorno.Servicio(db, variantes: true);

        var dto = new PromoReglaDetalleDto
        {
            Nombre = "3X2", Tipo = "NXM", Lleva = 3, Paga = 2, AgrupaVariantes = true,
            Desde = DateTime.Today, Hasta = DateTime.Today.AddDays(5), ProductoIds = v.ToList()
        };
        var id = await s.GuardarAsync(dto, "t");
        Assert.True((await s.ObtenerAsync(id))!.AgrupaVariantes);
        Assert.Contains("acumula variantes", (await s.ListarAsync()).Single().Resumen);
        Assert.True((await s.ListarAsync()).Single().AgrupaVariantes);

        dto.Id = id; dto.AgrupaVariantes = false;                      // se edita y se desmarca
        await s.GuardarAsync(dto, "t");
        Assert.False((await s.ObtenerAsync(id))!.AgrupaVariantes);
        Assert.DoesNotContain("acumula", (await s.ListarAsync()).Single().Resumen);

        var porc = new PromoReglaDetalleDto
        {
            Nombre = "10%", Tipo = "PORC", Porcentaje = 10m, AgrupaVariantes = true,    // la opción no existe para porcentaje
            Desde = DateTime.Today, Hasta = DateTime.Today.AddDays(5), ProductoIds = v.ToList()
        };
        var idP = await s.GuardarAsync(porc, "t");
        Assert.False((await s.ObtenerAsync(idP))!.AgrupaVariantes);
    }

    // ── Guardado real (pedido y venta) con acumulación ──────────────────────
    private const decimal Iva = 21m;
    private static PedidoService Pedidos(ComercialDbContext db) => new(db, new VariantesProductoService(db), Entorno.Servicio(db, variantes: true));
    private static VentaService Ventas(ComercialDbContext db) => new(db, null!, new VariantesProductoService(db), Entorno.Servicio(db, variantes: true));

    private static GuardarPedidoDetalleDto LP(int prod, decimal cant, decimal precio, decimal descuentoPct = 0, int? regla = null)
    {
        var subSin = precio * (1m - descuentoPct / 100m);
        var conIva = subSin * (1m + Iva / 100m);
        return new GuardarPedidoDetalleDto { FkProducto = prod, Cantidad = cant, PrecioSinIva = precio, PrecioOrig = precio, Costo = 1, Descripcion = "X",
            Descuento = descuentoPct, SubtotalSinIva = subSin, PrecioConIva = conIva, Subtotal = conIva * cant, FkPromoRegla = regla };
    }

    private static GuardarPedidoRequestDto Ped(params GuardarPedidoDetalleDto[] l)
        => new() { FkCliente = 1, Iva = Iva, FkVendedor = 1, Total = l.Sum(x => x.Subtotal), Detalle = l.ToList() };

    [Fact]
    public async Task Pedido_VariantesAcumuladas_PersisteLaBonificadaEnLaVarianteCorrecta()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        var r = await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, v, agrupa: true);

        // el navegador bonifica la primera variante (100 %); las otras dos a precio normal
        var dto = Ped(LP(v[0], 1, 10000, 100, r.Id), LP(v[1], 1, 10000), LP(v[2], 1, 10000));
        var id = await Pedidos(db).GuardarPedidoAsync(dto);

        var ls = await db.PedidoDetalles.Where(d => d.FkPedido == id).OrderBy(d => d.Linea).ToListAsync();
        Assert.Equal(3, ls.Count);
        Assert.Equal(v[0], ls[0].FkProducto); Assert.Equal(100m, ls[0].Descuento); Assert.Equal(0m, ls[0].Subtotal);
        Assert.Null(ls[1].Descuento); Assert.Null(ls[2].Descuento);
        Assert.Equal(2 * 10000m * 1.21m, ls.Sum(l => l.Subtotal!.Value));            // 3 unidades, se cobran 2
        var aud = Assert.Single(await db.PedidosPromoDetalle.ToListAsync());          // auditoría en la línea bonificada
        Assert.Equal(r.Id, aud.FkRegla); Assert.Equal(10000m, aud.Ahorro);
    }

    [Fact]
    public async Task Pedido_VariantesAcumuladas_RechazaSiElNavegadorNoLaAplicoOLaPusoEnOtraVariante()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        var r = await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, v, agrupa: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>             // no aplicó la promoción
            Pedidos(db).GuardarPedidoAsync(Ped(LP(v[0], 1, 10000), LP(v[1], 1, 10000), LP(v[2], 1, 10000))));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>             // la bonificó en otra variante
            Pedidos(db).GuardarPedidoAsync(Ped(LP(v[0], 1, 10000), LP(v[1], 1, 10000, 100, r.Id), LP(v[2], 1, 10000))));
        Assert.Empty(db.Pedidos);
    }

    [Fact]
    public async Task Pedido_VariantesSinAcumular_ComportamientoActual()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, v, agrupa: false);
        var id = await Pedidos(db).GuardarPedidoAsync(Ped(LP(v[0], 1, 10000), LP(v[1], 1, 10000), LP(v[2], 1, 10000)));
        var ls = await db.PedidoDetalles.Where(d => d.FkPedido == id).ToListAsync();
        Assert.Equal(3, ls.Count); Assert.All(ls, l => Assert.Null(l.Descuento));
        Assert.Empty(db.PedidosPromoDetalle);
    }

    [Fact]
    public async Task Venta_VariantesAcumuladas_MismosImportesQueElPedido_YBajaStockDeCadaVariante()
    {
        await using var db = Entorno.NuevaBd();
        await Entorno.ParametroAsync(db, "1");
        var v = await Entorno.VariantesAsync(db, "REMERA", 10000, "ROJA", "AZUL", "NEGRA");
        foreach (var id in v) db.StockProductos.Add(new Domain.Entities.StockProducto { FkProducto = id, Cantidad = 10 });
        await db.SaveChangesAsync();
        var r = await Entorno.ReglaAsync(db, "3X2", "NXM", null, 3, 2, v, agrupa: true);

        GrabarVentaDetalleDto LV(int prod, decimal descRec = 0, int? regla = null)
        {
            var sub = 10000m * (1m + descRec / 100m);
            return new GrabarVentaDetalleDto { FkProducto = prod, Cantidad = 1, PrecioSinIva = 10000m, PrecioConIva = sub * 1.21m, Costo = 1,
                DescRec = descRec, SubtotalSinIva = sub, Subtotal = sub * 1.21m, FkPromoRegla = regla };
        }
        var detalle = new List<GrabarVentaDetalleDto> { LV(v[0], -100, r.Id), LV(v[1]), LV(v[2]) };
        var dto = new GrabarVentaRequestDto { FkCliente = 1, Iva = Iva, FkVendedor = 1, Total = detalle.Sum(x => x.Subtotal), Costo = 1, Detalle = detalle };
        var ventaId = await Ventas(db).GrabarVentaAsync(dto);

        var ls = await db.VentasDetalles.Where(d => d.FkVenta == ventaId).OrderBy(d => d.Linea).ToListAsync();
        Assert.Equal(3, ls.Count);
        Assert.Equal(100m, ls[0].Descuento);
        Assert.Equal(2 * 10000m * 1.21m, ls.Sum(l => l.Subtotal!.Value));            // igual que en el pedido
        Assert.All(v, id => Assert.Equal(9m, db.StockProductos.Single(s => s.FkProducto == id).Cantidad));   // sale 1 unidad de cada variante
        Assert.Single(db.VentasPromoDetalle);
    }
}
