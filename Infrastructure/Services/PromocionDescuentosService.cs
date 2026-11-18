using System.Globalization;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Domain.Promociones;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Promociones por cantidad (NxM) y porcentaje. Orquesta acceso a datos alrededor del motor puro
/// <see cref="MotorPromociones"/>. No toca las promociones por combos (<see cref="PromocionService"/>).
/// </summary>
public class PromocionDescuentosService : IPromocionDescuentosService
{
    private const string Modulo    = "promociones";
    private const string Parametro = "descuentos";
    private static readonly TimeSpan VigenciaCache = TimeSpan.FromSeconds(60);
    private static readonly CultureInfo Ar = new("es-AR");

    // Estado compartido por el proceso: evita consultar parámetro/tablas en cada operación.
    private static (bool Habilitado, bool Tablas, DateTime Vence) _estado = (false, false, DateTime.MinValue);

    private readonly ComercialDbContext _db;
    private readonly IImpresionEtiquetasService _etiquetas;
    private readonly IVariantesProductoService  _variantes;

    public PromocionDescuentosService(ComercialDbContext db, IImpresionEtiquetasService etiquetas, IVariantesProductoService variantes)
    {
        _db        = db;
        _etiquetas = etiquetas;
        _variantes = variantes;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Estado
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<bool> HabilitadoAsync()    => (await EstadoAsync()).Habilitado;
    public async Task<bool> TablasListasAsync()  => (await EstadoAsync()).Tablas;

    private async Task<(bool Habilitado, bool Tablas)> EstadoAsync()
    {
        var actual = _estado;
        if (UsarCache && actual.Vence > DateTime.UtcNow) return (actual.Habilitado, actual.Tablas);

        var valor = await _db.Parametros.AsNoTracking()
            .Where(p => p.Modulo == Modulo && p.Parametro1 == Parametro)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync();

        // Las tablas solo se buscan en information_schema: con el parámetro apagado y sin tablas
        // no se consulta ninguna estructura nueva.
        // Tablas del módulo + columna agrupaVariantes (si falta, el módulo queda inerte en vez de fallar al consultar)
        var tablas = await ContarTablasAsync() == 4 && await ColumnaAgrupaVariantesExisteAsync();

        var habilitado = tablas && valor?.Trim() == "1";
        if (UsarCache) _estado = (habilitado, tablas, DateTime.UtcNow + VigenciaCache);
        return (habilitado, tablas);
    }

    // Costuras para las pruebas automáticas (EF en memoria no tiene information_schema ni conviene cachear entre casos).
    protected virtual bool UsarCache => true;

    protected virtual async Task<bool> ColumnaAgrupaVariantesExisteAsync()
        => (await _db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'promociones_reglas' AND LOWER(COLUMN_NAME) = 'agrupavariantes'")
            .ToListAsync()).FirstOrDefault() == 1;

    protected virtual async Task<int> ContarTablasAsync()
        => (await _db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS `Value` FROM information_schema.TABLES " +
            "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) IN " +
            "('promociones_reglas','promociones_reglas_productos','ventas_promo_detalle','pedidos_promo_detalle')")
            .ToListAsync()).FirstOrDefault();

    // ═══════════════════════════════════════════════════════════════════════
    // Reglas vigentes para un conjunto de productos
    // ═══════════════════════════════════════════════════════════════════════
    private async Task<List<ReglaPromo>> ReglasVigentesAsync(IReadOnlyCollection<int> productoIds, DateTime ahora)
    {
        if (productoIds.Count == 0) return new List<ReglaPromo>();

        var ids = productoIds.ToList();
        var asoc = await (
            from rp in _db.PromocionesReglasProductos
            join r  in _db.PromocionesReglas on rp.FkRegla equals r.Id
            where ids.Contains(rp.FkProducto)
                  && r.Activa && r.VigenteDesde <= ahora && r.VigenteHasta >= ahora
            select new { rp.FkRegla, rp.FkProducto }
        ).AsNoTracking().ToListAsync();

        if (asoc.Count == 0) return new List<ReglaPromo>();

        var reglaIds = asoc.Select(a => a.FkRegla).Distinct().ToList();
        var reglas = await _db.PromocionesReglas.AsNoTracking()
            .Where(r => reglaIds.Contains(r.Id)).ToListAsync();

        return reglas.Select(r => new ReglaPromo
        {
            Id          = r.Id,
            Nombre      = r.Nombre,
            Tipo        = r.Tipo,
            Porcentaje  = r.Porcentaje,
            Lleva       = r.Lleva,
            Paga        = r.Paga,
            AgrupaVariantes = r.Tipo == TipoPromo.NxM && r.AgrupaVariantes,
            Desde       = r.VigenteDesde,
            Hasta       = r.VigenteHasta,
            Activa      = r.Activa,
            ProductoIds = asoc.Where(a => a.FkRegla == r.Id).Select(a => a.FkProducto).ToHashSet()
        }).ToList();
    }

    // Los productos fraccionados (importe por peso/medida) quedan fuera del motor: su importe es libre.
    private async Task<HashSet<int>> FraccionadosAsync(IReadOnlyCollection<int> productoIds)
    {
        if (productoIds.Count == 0) return new HashSet<int>();
        var ids = productoIds.ToList();
        var lista = await _db.Productos.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.Fraccionado == true)
            .Select(p => p.Id).ToListAsync();
        return lista.ToHashSet();
    }

    // Líneas válidas (sin fraccionados) + reglas vigentes: una sola carga que usan Evaluar y Proximas.
    private async Task<(List<LineaPromoEntrada> Validas, List<ReglaPromo> Reglas, DateTime Ahora)> PrepararAsync(
        IEnumerable<LineaPromoEntrada> entrada)
    {
        var ahora  = HoraArgentina.Ahora();
        var lineas = entrada.Where(l => l.Cantidad > 0 && l.PrecioSinIva > 0 && l.ProductoId > 0).ToList();
        if (lineas.Count == 0) return (new List<LineaPromoEntrada>(), new List<ReglaPromo>(), ahora);

        var prodIds = lineas.Select(l => l.ProductoId).Distinct().ToList();
        var fracc   = await FraccionadosAsync(prodIds);
        var validas = lineas.Where(l => !fracc.Contains(l.ProductoId)).ToList();
        if (validas.Count == 0) return (validas, new List<ReglaPromo>(), ahora);

        var reglas = await ReglasVigentesAsync(validas.Select(l => l.ProductoId).Distinct().ToList(), ahora);
        await AsignarFichasAsync(validas, reglas);
        return (validas, reglas, ahora);
    }

    // Ficha de cada variante, SOLO si alguna regla vigente acumula variantes y el sistema trabaja con variantes:
    // en cualquier otro caso no se consulta ninguna tabla de variantes y el cálculo es el de siempre.
    private async Task AsignarFichasAsync(List<LineaPromoEntrada> lineas, List<ReglaPromo> reglas)
    {
        var agrupadas = reglas.Where(r => r.AgrupaVariantes && r.Tipo == TipoPromo.NxM).ToList();
        if (agrupadas.Count == 0 || !await _variantes.HabilitadoAsync()) return;

        var enReglas = agrupadas.SelectMany(r => r.ProductoIds).ToHashSet();
        var ids = lineas.Select(l => l.ProductoId).Distinct().Where(enReglas.Contains).ToList();
        if (ids.Count == 0) return;

        var fichas = await _db.ProductosVariantes.AsNoTracking()
            .Where(v => ids.Contains(v.FkProducto))
            .Select(v => new { v.FkProducto, v.FkPrincipal })
            .ToDictionaryAsync(v => v.FkProducto, v => v.FkPrincipal);
        foreach (var l in lineas)
            if (fichas.TryGetValue(l.ProductoId, out var ficha)) l.FichaId = ficha;
    }

    private async Task<List<LineaPromoResultado>> EvaluarInternoAsync(IEnumerable<LineaPromoEntrada> entrada)
    {
        var (validas, reglas, ahora) = await PrepararAsync(entrada);
        return validas.Count == 0 ? new List<LineaPromoResultado>() : MotorPromociones.Evaluar(validas, reglas, ahora);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Evaluación desde la grilla
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<PromoEvalRespuestaDto> EvaluarAsync(IEnumerable<PromoEvalLineaDto> lineas)
    {
        var resp = new PromoEvalRespuestaDto();
        if (!await HabilitadoAsync()) return resp;
        resp.Habilitado = true;

        var (validas, reglas, ahora) = await PrepararAsync(lineas.Select(l => new LineaPromoEntrada
        {
            Clave = l.Clave, ProductoId = l.FkProducto, Cantidad = l.Cantidad, PrecioSinIva = l.PrecioSinIva
        }));
        if (validas.Count == 0) return resp;

        var res = MotorPromociones.Evaluar(validas, reglas, ahora);

        // Avisos de proximidad (informativos): misma carga de reglas, sin consultas adicionales de promociones.
        var proximas = MotorPromociones.Proximas(validas, reglas, ahora);
        if (proximas.Count > 0)
        {
            var idsProx = proximas.Select(p => p.ProductoId).Distinct().ToList();
            var nomProx = await _db.Productos.AsNoTracking()
                .Where(p => idsProx.Contains(p.Id))
                .Select(p => new { p.Id, p.Descripcion })
                .ToDictionaryAsync(p => p.Id, p => p.Descripcion ?? "");
            resp.Proximas = proximas.Select(p => new PromoProximaDto
            {
                Clave = p.Clave, FkProducto = p.ProductoId, FkRegla = p.ReglaId, Nombre = p.Nombre,
                Lleva = p.Lleva, Paga = p.Paga, CantidadActual = p.CantidadActual,
                Descripcion = nomProx.GetValueOrDefault(p.ProductoId, ""),
                Texto = p.Agrupa
                    ? $"¡Aprovechá la promoción {p.Nombre}! Agregando 1 unidad más de cualquier variante de esta ficha incluida en la promoción, llevás {p.Lleva} y pagás {p.Paga}."
                    : $"¡Aprovechá la promoción {p.Nombre}! Agregando 1 unidad más, llevás {p.Lleva} y pagás {p.Paga}.",
                TextoCorto = $"¡+1 y llevá {p.Lleva} pagando {p.Paga}!"
            }).ToList();
        }
        if (res.Count == 0) return resp;

        resp.Lineas = res.Select(r => new PromoEvalLineaResultadoDto
        {
            Clave = r.Clave, FkProducto = r.ProductoId, FkRegla = r.ReglaId, Nombre = r.Nombre, Tipo = r.Tipo,
            Porcentaje = r.Porcentaje, Lleva = r.Lleva, Paga = r.Paga,
            CantBonificada = r.CantBonificada, DescuentoPct = r.DescuentoPct, Ahorro = r.Ahorro
        }).ToList();

        // Resumen para el usuario: una entrada por producto, solo con promociones realmente aplicadas.
        var prodIds = res.Select(r => r.ProductoId).Distinct().ToList();
        var nombres = await _db.Productos.AsNoTracking()
            .Where(p => prodIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Descripcion })
            .ToDictionaryAsync(p => p.Id, p => p.Descripcion ?? "");

        foreach (var g in res.GroupBy(r => r.ProductoId))
        {
            var r0     = g.First();
            var ahorro = g.Sum(x => x.Ahorro);
            var bonif  = g.Sum(x => x.CantBonificada);
            string texto = r0.Tipo == TipoPromo.NxM
                ? $"promoción {r0.Lleva}x{r0.Paga}{(r0.AgrupaVariantes ? " acumulando variantes" : "")} ({bonif.ToString("0.####", Ar)} unidad{(bonif == 1m ? "" : "es")} bonificada{(bonif == 1m ? "" : "s")}, ahorro {ahorro.ToString("C2", Ar)} sin IVA)"
                : $"{r0.Porcentaje!.Value.ToString("0.##", Ar)}% de descuento (ahorro {ahorro.ToString("C2", Ar)} sin IVA)";

            resp.Resumen.Add(new PromoResumenDto
            {
                FkProducto  = g.Key,
                Descripcion = nombres.GetValueOrDefault(g.Key, ""),
                Texto       = texto
            });
        }
        return resp;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Guardado: validación (el servidor manda) + expansión NxM
    // ═══════════════════════════════════════════════════════════════════════
    public async Task<List<PromoGuardarSegmento>> ResolverParaGuardarAsync(IReadOnlyList<PromoGuardarLinea> lineas)
    {
        // Sin motor: sin cambios (una línea = un segmento sin tocar)
        List<PromoGuardarSegmento> SinCambios() => lineas.Select(l => new PromoGuardarSegmento
        {
            Indice = l.Indice, Cantidad = l.Cantidad
        }).ToList();

        if (!await HabilitadoAsync()) return SinCambios();

        // Elegibles: no excluidas, y sin descuento manual (descuento ≠ 0 sin promoción declarada = manual: gana el manual).
        var elegibles = lineas.Where(l => !l.Excluida && l.Cantidad > 0 && l.PrecioSinIva > 0 &&
                                          (l.DescRec == 0m || l.FkPromoRegla.HasValue)).ToList();

        var res = await EvaluarInternoAsync(elegibles.Select(l => new LineaPromoEntrada
        {
            Clave = l.Indice, ProductoId = l.FkProducto, Cantidad = l.Cantidad, PrecioSinIva = l.PrecioSinIva
        }));
        var porIndice = res.ToDictionary(r => r.Clave);

        const string msg = "Las promociones vigentes cambiaron mientras se armaba la operación. " +
                           "Se recalcularon los importes en pantalla: revíselos y vuelva a confirmar.";

        foreach (var l in elegibles)
        {
            porIndice.TryGetValue(l.Indice, out var esperado);
            if (esperado == null)
            {
                if (l.FkPromoRegla.HasValue) throw new InvalidOperationException(msg);   // promoción que ya no corresponde
                continue;
            }
            // Corresponde promoción: el navegador debe haber informado la misma, con el mismo descuento efectivo.
            if (l.FkPromoRegla != esperado.ReglaId ||
                Math.Abs(-esperado.DescuentoPct - l.DescRec) > 0.001m)
                throw new InvalidOperationException(msg);
        }

        var segmentos = new List<PromoGuardarSegmento>();
        foreach (var l in lineas)
        {
            if (!porIndice.TryGetValue(l.Indice, out var r) || !elegibles.Contains(l))
            {
                segmentos.Add(new PromoGuardarSegmento { Indice = l.Indice, Cantidad = l.Cantidad });
                continue;
            }

            if (r.Tipo == TipoPromo.Porcentaje)
            {
                segmentos.Add(new PromoGuardarSegmento
                {
                    Indice = l.Indice, Cantidad = l.Cantidad, DescuentoPct = r.DescuentoPct,
                    Promo = Aplicada(r)
                });
            }
            else   // NxM: cobradas (precio normal) + bonificadas (100 %)
            {
                var bonif  = r.CantBonificada;
                var cobradas = l.Cantidad - bonif;
                var promo  = Aplicada(r);
                if (cobradas > 0m)
                    segmentos.Add(new PromoGuardarSegmento
                    {
                        Indice = l.Indice, Cantidad = cobradas, DescuentoPct = 0m,
                        Promo = new PromoAplicadaDto
                        {
                            FkRegla = promo.FkRegla, Nombre = promo.Nombre, Tipo = promo.Tipo,
                            Lleva = promo.Lleva, Paga = promo.Paga, CantBonificada = bonif, Ahorro = 0m
                        }
                    });
                segmentos.Add(new PromoGuardarSegmento
                {
                    Indice = l.Indice, Cantidad = bonif, DescuentoPct = 100m, Bonificada = true, Promo = promo
                });
            }
        }
        return segmentos;
    }

    private static PromoAplicadaDto Aplicada(LineaPromoResultado r) => new()
    {
        FkRegla = r.ReglaId, Nombre = r.Nombre, Tipo = r.Tipo, Porcentaje = r.Porcentaje,
        Lleva = r.Lleva, Paga = r.Paga, CantBonificada = r.CantBonificada, Ahorro = r.Ahorro
    };

    public async Task<Dictionary<long, int>> ReglaPorLineaPedidoAsync(IEnumerable<long> lineasPedido)
    {
        var lineas = lineasPedido.Distinct().ToList();
        if (lineas.Count == 0 || !await HabilitadoAsync()) return new Dictionary<long, int>();

        var filas = await _db.PedidosPromoDetalle.AsNoTracking()
            .Where(p => lineas.Contains(p.FkPedidoDetalle) && p.FkRegla != null)
            .Select(p => new { p.FkPedidoDetalle, Regla = p.FkRegla!.Value })
            .ToListAsync();
        return filas.GroupBy(f => f.FkPedidoDetalle).ToDictionary(g => g.Key, g => g.First().Regla);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ABM
    // ═══════════════════════════════════════════════════════════════════════
    private static string Resumen(string tipo, decimal? pct, int? lleva, int? paga)
        => tipo == TipoPromo.NxM
            ? $"{lleva}x{paga}"
            : $"{(pct ?? 0m).ToString("0.##", Ar)} % de descuento";

    public async Task<List<PromoReglaListDto>> ListarAsync()
    {
        if (!await TablasListasAsync()) return new List<PromoReglaListDto>();

        var reglas = await _db.PromocionesReglas.AsNoTracking()
            .OrderByDescending(r => r.VigenteHasta).ThenBy(r => r.Nombre).ToListAsync();
        var cuentas = await _db.PromocionesReglasProductos.AsNoTracking()
            .GroupBy(p => p.FkRegla)
            .Select(g => new { Regla = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Regla, x => x.Total);

        var ahora = HoraArgentina.Ahora();
        return reglas.Select(r => new PromoReglaListDto
        {
            Id = r.Id, Nombre = r.Nombre, Tipo = r.Tipo,
            Resumen = Resumen(r.Tipo, r.Porcentaje, r.Lleva, r.Paga) + (r.Tipo == TipoPromo.NxM && r.AgrupaVariantes ? " · acumula variantes" : ""),
            AgrupaVariantes = r.Tipo == TipoPromo.NxM && r.AgrupaVariantes,
            Desde = r.VigenteDesde, Hasta = r.VigenteHasta, Activa = r.Activa,
            Vigente = r.Activa && r.VigenteDesde <= ahora && r.VigenteHasta >= ahora,
            CantProductos = cuentas.GetValueOrDefault(r.Id, 0)
        }).ToList();
    }

    public async Task<PromoReglaDetalleDto?> ObtenerAsync(int id)
    {
        if (!await TablasListasAsync()) return null;
        var r = await _db.PromocionesReglas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return null;

        var ids = await _db.PromocionesReglasProductos.AsNoTracking()
            .Where(p => p.FkRegla == id).Select(p => p.FkProducto).ToListAsync();

        return new PromoReglaDetalleDto
        {
            Id = r.Id, Nombre = r.Nombre, Tipo = r.Tipo, Porcentaje = r.Porcentaje,
            Lleva = r.Lleva, Paga = r.Paga, AgrupaVariantes = r.Tipo == TipoPromo.NxM && r.AgrupaVariantes,
            Desde = r.VigenteDesde, Hasta = r.VigenteHasta, Activa = r.Activa,
            ProductoIds = ids,
            Productos   = await ProductosConPrecioAsync(ids)
        };
    }

    public async Task<int> GuardarAsync(PromoReglaDetalleDto dto, string? usuario)
    {
        if (!await TablasListasAsync())
            throw new InvalidOperationException("Faltan crear las tablas de promociones (ejecute scripts/promociones_reglas.sql).");

        // ── Validaciones (también en el servidor: en MySQL 5.5 los CHECK no se aplican) ──
        var nombre = (dto.Nombre ?? "").Trim().ToUpperInvariant();
        if (nombre.Length == 0)   throw new InvalidOperationException("Ingrese el nombre de la promoción.");
        if (nombre.Length > 100)  throw new InvalidOperationException("El nombre no puede superar los 100 caracteres.");

        var tipo = (dto.Tipo ?? "").Trim().ToUpperInvariant();
        decimal? pct = null; int? lleva = null, paga = null;
        if (tipo == TipoPromo.Porcentaje)
        {
            if (!dto.Porcentaje.HasValue || dto.Porcentaje.Value <= 0m || dto.Porcentaje.Value > 100m)
                throw new InvalidOperationException("El porcentaje de descuento debe ser mayor que 0 y como máximo 100.");
            if (decimal.Round(dto.Porcentaje.Value, 2) != dto.Porcentaje.Value)
                throw new InvalidOperationException("El porcentaje admite como máximo 2 decimales.");
            pct = dto.Porcentaje.Value;
        }
        else if (tipo == TipoPromo.NxM)
        {
            if (!dto.Lleva.HasValue || !dto.Paga.HasValue)
                throw new InvalidOperationException("Indique cuántas unidades lleva y cuántas paga el cliente.");
            if (dto.Lleva.Value < 2 || dto.Lleva.Value > 99)
                throw new InvalidOperationException("La cantidad que lleva el cliente debe estar entre 2 y 99.");
            if (dto.Paga.Value < 1 || dto.Paga.Value >= dto.Lleva.Value)
                throw new InvalidOperationException("La cantidad que paga debe ser al menos 1 y menor que la que lleva.");
            lleva = dto.Lleva; paga = dto.Paga;
        }
        else throw new InvalidOperationException("Tipo de promoción inválido.");

        if (dto.Hasta < dto.Desde)
            throw new InvalidOperationException("La fecha de finalización no puede ser anterior a la de inicio.");

        var prodIds = dto.ProductoIds.Where(i => i > 0).Distinct().ToList();
        if (prodIds.Count == 0) throw new InvalidOperationException("Seleccione al menos un producto.");
        var existentes = await _db.Productos.AsNoTracking().Where(p => prodIds.Contains(p.Id)).CountAsync();
        if (existentes != prodIds.Count) throw new InvalidOperationException("Hay productos seleccionados que ya no existen.");

        // Segundos y milisegundos fuera: la vigencia se maneja al minuto
        DateTime Min(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            PromocionRegla regla;
            if (dto.Id > 0)
            {
                regla = await _db.PromocionesReglas.FirstOrDefaultAsync(r => r.Id == dto.Id)
                        ?? throw new InvalidOperationException("La promoción no existe.");
            }
            else
            {
                regla = new PromocionRegla { FechaAlta = HoraArgentina.Ahora() };
                _db.PromocionesReglas.Add(regla);
            }
            regla.Nombre = nombre; regla.Tipo = tipo; regla.Porcentaje = pct; regla.Lleva = lleva; regla.Paga = paga;
            regla.AgrupaVariantes = tipo == TipoPromo.NxM && dto.AgrupaVariantes;   // solo existe para NxM
            regla.VigenteDesde = Min(dto.Desde); regla.VigenteHasta = Min(dto.Hasta);
            regla.Activa = dto.Activa; regla.Usuario = usuario;
            await _db.SaveChangesAsync();

            var viejos = await _db.PromocionesReglasProductos.Where(p => p.FkRegla == regla.Id).ToListAsync();
            _db.PromocionesReglasProductos.RemoveRange(viejos);
            await _db.SaveChangesAsync();
            _db.PromocionesReglasProductos.AddRange(prodIds.Select(id => new PromocionReglaProducto
            {
                FkRegla = regla.Id, FkProducto = id
            }));
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return regla.Id;
        }
        catch { await tx.RollbackAsync(); throw; }
    }

    public async Task EliminarAsync(int id)
    {
        if (!await TablasListasAsync()) return;
        var regla = await _db.PromocionesReglas.FirstOrDefaultAsync(r => r.Id == id);
        if (regla == null) return;
        _db.PromocionesReglasProductos.RemoveRange(_db.PromocionesReglasProductos.Where(p => p.FkRegla == id));
        _db.PromocionesReglas.Remove(regla);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ToggleActivaAsync(int id)
    {
        var regla = await _db.PromocionesReglas.FirstOrDefaultAsync(r => r.Id == id)
                    ?? throw new InvalidOperationException("La promoción no existe.");
        regla.Activa = !regla.Activa;
        await _db.SaveChangesAsync();
        return regla.Activa;
    }

    // ── Búsqueda de productos: reutiliza el buscador de etiquetas/lista de precios ──
    // (proveedor, rubro con subrubros, texto sobre cód. barras / cód. proveedor / descripción;
    //  con variantes habilitadas excluye las fichas principales).
    public async Task<PromoBusquedaProductosDto> BuscarProductosAsync(
        IEnumerable<int>? proveedores, IEnumerable<int>? rubros, string? texto, int max)
    {
        var provs = proveedores?.Where(i => i > 0).ToList();
        var rubs  = rubros?.Where(i => i > 0).ToList();
        var todo  = await _etiquetas.BuscarAsync(
            provs is { Count: > 0 } ? provs : null,
            rubs  is { Count: > 0 } ? rubs  : null,
            texto);

        var pagina = todo.Take(Math.Max(1, max)).ToList();
        var precios = await PreciosVigentesAsync(pagina.Select(p => p.ProductoId).ToList());

        return new PromoBusquedaProductosDto
        {
            Total = todo.Count,
            Items = pagina.Select(p => new PromoReglaProductoDto
            {
                ProductoId   = p.ProductoId,
                CodProveedor = p.CodProveedor,
                CodBarras    = p.CodBarras,
                Descripcion  = p.Descripcion,
                Proveedor    = p.ProveedorNombre,
                Rubro        = p.RubroNombre,
                PrecioActual = precios.GetValueOrDefault(p.ProductoId, 0m)
            }).ToList()
        };
    }

    // ── Precio vigente sin IVA: misma regla que ventas y pedidos (dolarización incluida) ──
    private async Task<Dictionary<int, decimal>> PreciosVigentesAsync(IReadOnlyCollection<int> ids)
    {
        var res = new Dictionary<int, decimal>();
        if (ids.Count == 0) return res;
        var lista = ids.ToList();

        string? Param(string modulo, string par) => _db.Parametros.AsNoTracking()
            .Where(p => p.Modulo == modulo && p.Parametro1 == par).Select(p => p.Valor).FirstOrDefault();

        var dolariza = int.TryParse(Param("productos", "dolarizaProductos"), out var d) && d == 1;
        decimal.TryParse(Param("productos", "cotizacionDolar"), NumberStyles.Any, CultureInfo.InvariantCulture, out var cotiz);

        var precios = await _db.PreciosProductos.AsNoTracking()
            .Where(pp => pp.FkProducto != null && lista.Contains(pp.FkProducto.Value))
            .Select(pp => new { Id = pp.FkProducto!.Value, pp.Precio }).ToListAsync();
        var dolarizados = (await _db.Productos.AsNoTracking()
            .Where(p => lista.Contains(p.Id) && p.Dolarizado == true).Select(p => p.Id).ToListAsync()).ToHashSet();

        foreach (var id in lista)
        {
            var precio = precios.FirstOrDefault(x => x.Id == id)?.Precio ?? 0m;
            if (dolariza && dolarizados.Contains(id) && cotiz > 0m) precio *= cotiz;
            res[id] = precio;
        }
        return res;
    }

    private async Task<List<PromoReglaProductoDto>> ProductosConPrecioAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0) return new List<PromoReglaProductoDto>();
        var lista = ids.ToList();

        var filas = await (
            from p in _db.Productos.AsNoTracking()
            where lista.Contains(p.Id)
            join pv in _db.Proveedores on p.FkProveedor equals (int?)pv.Id into pvG from pv in pvG.DefaultIfEmpty()
            join rb in _db.Rubros      on p.FkRubro     equals (int?)rb.Id into rbG from rb in rbG.DefaultIfEmpty()
            orderby p.Descripcion
            select new
            {
                p.Id, p.CodProveedor, p.CodBarras, p.Descripcion,
                Proveedor = pv != null ? pv.NombreComercial : null,
                Rubro     = rb != null ? rb.Descripcion     : null
            }).ToListAsync();

        var precios = await PreciosVigentesAsync(lista);
        return filas.Select(f => new PromoReglaProductoDto
        {
            ProductoId = f.Id, CodProveedor = f.CodProveedor, CodBarras = f.CodBarras,
            Descripcion = f.Descripcion ?? "", Proveedor = f.Proveedor, Rubro = f.Rubro,
            PrecioActual = precios.GetValueOrDefault(f.Id, 0m)
        }).ToList();
    }
}
