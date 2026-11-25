using System.Text.RegularExpressions;
using Domain.Contracts;
using Domain.DTO;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Productos con variantes (medidas y/o colores), con la misma lógica que LitoralMarket. Cada variante ES una fila
/// de <c>Productos</c> con su propio stock, precio y costo, enlazada a una ficha principal
/// (<c>productosPrincipales</c>/<c>productosVariantes</c>). Para ventas, pedidos, stock y reportes una variante
/// es un producto común.
/// </summary>
public class VarianteAdminService : IVarianteAdminService
{
    private static readonly Regex HexValido = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    private const int MaxNombreAtributo = 40;
    private const int MaxDescripcion    = 150;   // Productos.descripcion es VARCHAR(150)
    private const int MaxCodigo         = 45;    // Productos.codBarras / codProveedor son VARCHAR(45)

    private readonly ComercialDbContext             _db;
    private readonly IProductoService               _productos;
    private readonly IVariantesProductoService      _estado;
    private readonly ILogger<VarianteAdminService>  _logger;
    private readonly IGeneradorCodigoBarrasService  _codigos;

    public VarianteAdminService(ComercialDbContext db, IProductoService productos,
        IVariantesProductoService estado, ILogger<VarianteAdminService> logger, IGeneradorCodigoBarrasService codigos)
    {
        _db        = db;
        _productos = productos;
        _estado    = estado;
        _logger    = logger;
        _codigos   = codigos;
    }

    // ═════════════════════════════════════════════════════════════
    // Habilitación
    // ═════════════════════════════════════════════════════════════
    public async Task<bool> HabilitadoAsync() =>
        await _estado.HabilitadoAsync() && await TablasListasAsync();

    // Resultado compartido por todo el proceso durante 60 s (HabilitadoAsync se consulta en cada búsqueda).
    private static (bool Listas, DateTime Vence) _tablas = (false, DateTime.MinValue);

    private async Task<bool> TablasListasAsync()
    {
        var cache = _tablas;
        if (cache.Vence > DateTime.UtcNow) return cache.Listas;

        bool listas;
        try
        {
            await _db.VariantesMedidas.AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
            await _db.VariantesColores.AsNoTracking().Select(x => x.Id).FirstOrDefaultAsync();
            await _db.ProductosPrincipales.AsNoTracking().Select(x => x.FkProducto).FirstOrDefaultAsync();
            await _db.ProductosVariantes.AsNoTracking().Select(x => x.FkProducto).FirstOrDefaultAsync();
            listas = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Variantes: faltan tablas (¿se corrió variantes_01_tablas.sql?)");
            listas = false;
        }
        _tablas = (listas, DateTime.UtcNow.AddSeconds(60));
        return listas;
    }

    // ═════════════════════════════════════════════════════════════
    // Maestros: medidas y colores
    // ═════════════════════════════════════════════════════════════
    public async Task<List<AtributoVarianteDto>> ListarMedidasAsync(bool soloActivas = false)
    {
        var q = _db.VariantesMedidas.AsNoTracking().AsQueryable();
        if (soloActivas) q = q.Where(m => m.Activa);
        return await q.OrderBy(m => m.Orden).ThenBy(m => m.Nombre)
            .Select(m => new AtributoVarianteDto
            {
                Id = m.Id, Nombre = m.Nombre, Orden = m.Orden, Activa = m.Activa,
                EnUso = _db.ProductosVariantes.Count(v => v.FkMedida == m.Id)
            })
            .ToListAsync();
    }

    public async Task<List<AtributoVarianteDto>> ListarColoresAsync(bool soloActivas = false)
    {
        var q = _db.VariantesColores.AsNoTracking().AsQueryable();
        if (soloActivas) q = q.Where(c => c.Activa);
        return await q.OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .Select(c => new AtributoVarianteDto
            {
                Id = c.Id, Nombre = c.Nombre, CodigoHex = c.CodigoHex, Orden = c.Orden, Activa = c.Activa,
                EnUso = _db.ProductosVariantes.Count(v => v.FkColor == c.Id)
            })
            .ToListAsync();
    }

    public async Task<ResultadoAtributo> GuardarMedidaAsync(int? id, string nombre, int orden, bool activa)
    {
        var error = ValidarNombreAtributo(ref nombre, "medida");
        if (error is not null) return ResultadoAtributo.Fallo(error);

        var lower  = nombre.ToLower();
        var propio = id ?? 0;
        if (await _db.VariantesMedidas.AnyAsync(m => m.Id != propio && m.Nombre.ToLower() == lower))
            return ResultadoAtributo.Fallo("Ya existe una medida con ese nombre.");

        VarianteMedida entidad;
        var renombrada = false;
        if (propio > 0)
        {
            entidad = await _db.VariantesMedidas.FirstOrDefaultAsync(m => m.Id == propio)
                      ?? throw new InvalidOperationException("La medida no existe.");
            renombrada = entidad.Nombre != nombre;
        }
        else
        {
            entidad = new VarianteMedida();
            _db.VariantesMedidas.Add(entidad);
        }
        entidad.Nombre = nombre;
        entidad.Orden  = orden;
        entidad.Activa = activa;
        await _db.SaveChangesAsync();

        if (renombrada) await RegenerarDescripcionesDeAtributoAsync(medidaId: entidad.Id, colorId: null);
        return ResultadoAtributo.Exito(entidad.Id);
    }

    public async Task<ResultadoAtributo> GuardarColorAsync(int? id, string nombre, string? codigoHex, int orden, bool activa)
    {
        var error = ValidarNombreAtributo(ref nombre, "color");
        if (error is not null) return ResultadoAtributo.Fallo(error);

        codigoHex = string.IsNullOrWhiteSpace(codigoHex) ? null : codigoHex.Trim();
        if (codigoHex is not null && !HexValido.IsMatch(codigoHex))
            return ResultadoAtributo.Fallo("El color debe tener el formato #RRGGBB (por ejemplo #1F2937).");

        var lower  = nombre.ToLower();
        var propio = id ?? 0;
        if (await _db.VariantesColores.AnyAsync(c => c.Id != propio && c.Nombre.ToLower() == lower))
            return ResultadoAtributo.Fallo("Ya existe un color con ese nombre.");

        VarianteColor entidad;
        var renombrado = false;
        if (propio > 0)
        {
            entidad = await _db.VariantesColores.FirstOrDefaultAsync(c => c.Id == propio)
                      ?? throw new InvalidOperationException("El color no existe.");
            renombrado = entidad.Nombre != nombre;
        }
        else
        {
            entidad = new VarianteColor();
            _db.VariantesColores.Add(entidad);
        }
        entidad.Nombre    = nombre;
        entidad.CodigoHex = codigoHex;
        entidad.Orden     = orden;
        entidad.Activa    = activa;
        await _db.SaveChangesAsync();

        if (renombrado) await RegenerarDescripcionesDeAtributoAsync(medidaId: null, colorId: entidad.Id);
        return ResultadoAtributo.Exito(entidad.Id);
    }

    private static string? ValidarNombreAtributo(ref string nombre, string que)
    {
        nombre = (nombre ?? string.Empty).Trim();
        if (nombre.Length == 0) return $"Escribí el nombre de la {que}.";
        if (nombre.Length > MaxNombreAtributo) return $"El nombre admite hasta {MaxNombreAtributo} caracteres.";
        // El nombre se escribe en Productos.descripcion (latin1): sin emojis ni caracteres fuera de ese juego.
        if (!EsLatin1(nombre)) return "El nombre solo admite letras, números y símbolos comunes (sin emojis).";
        return null;
    }

    /// <summary>Renombrar una medida/color regenera la descripción de las variantes que la usan.</summary>
    private async Task RegenerarDescripcionesDeAtributoAsync(int? medidaId, int? colorId)
    {
        var principalIds = await _db.ProductosVariantes
            .Where(v => (medidaId != null && v.FkMedida == medidaId) || (colorId != null && v.FkColor == colorId))
            .Select(v => v.FkPrincipal)
            .Distinct()
            .ToListAsync();
        if (principalIds.Count == 0) return;

        var medidas = await _db.VariantesMedidas.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Nombre);
        var colores = await _db.VariantesColores.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombre);

        foreach (var principalId in principalIds)
        {
            var ficha = await _db.ProductosPrincipales.FirstOrDefaultAsync(x => x.FkProducto == principalId);
            var nombreBase = await _db.Productos.Where(x => x.Id == principalId).Select(x => x.Descripcion).FirstOrDefaultAsync();
            if (ficha is null || string.IsNullOrWhiteSpace(nombreBase)) continue;

            var variantes = await _db.ProductosVariantes.Where(v => v.FkPrincipal == principalId).ToListAsync();
            var ids       = variantes.Select(v => v.FkProducto).ToList();
            var productos = await _db.Productos.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id);

            foreach (var v in variantes)
            {
                var desc = Generar(nombreBase, ficha.UsaMedidas, ficha.UsaColores,
                    medidas.GetValueOrDefault(v.FkMedida), colores.GetValueOrDefault(v.FkColor));
                if (productos.TryGetValue(v.FkProducto, out var prod) && desc.Length <= MaxDescripcion && EsLatin1(desc))
                    prod.Descripcion = desc;
            }
        }
        await _db.SaveChangesAsync();
    }

    // ═════════════════════════════════════════════════════════════
    // Listado del ABM: una fila por producto principal
    // ═════════════════════════════════════════════════════════════
    public async Task<ResultadoListadoVariantes> ResolverListadoAsync(IReadOnlyList<Producto> productos)
    {
        var res = new ResultadoListadoVariantes { Productos = productos.ToList() };
        if (productos.Count == 0 || !await HabilitadoAsync()) return res;

        // 1) ¿cuáles de los encontrados son variantes? (y de qué ficha)
        var ids = productos.Select(p => p.Id).ToList();
        var deVariantes = await _db.ProductosVariantes.AsNoTracking()
            .Where(v => ids.Contains(v.FkProducto))
            .Select(v => new { v.FkProducto, v.FkPrincipal })
            .ToListAsync();
        var idsVariantes = deVariantes.Select(x => x.FkProducto).ToHashSet();

        // 2) Las variantes se ven por su ficha: se quitan de la lista y se agrega la ficha si no estaba.
        var lista   = productos.Where(p => !idsVariantes.Contains(p.Id)).ToList();
        var yaEstan = lista.Select(p => p.Id).ToHashSet();
        var faltan  = deVariantes.Select(x => x.FkPrincipal).Distinct().Where(i => !yaEstan.Contains(i)).ToList();
        if (faltan.Count > 0)
        {
            // Solo las columnas que usa el listado (evita traer el blob de la imagen)
            var fichasExtra = await _db.Productos.AsNoTracking()
                .Where(p => faltan.Contains(p.Id) && p.Baja != true)
                .Select(p => new Producto { Id = p.Id, CodProveedor = p.CodProveedor, CodBarras = p.CodBarras, Descripcion = p.Descripcion })
                .ToListAsync();
            if (fichasExtra.Count > 0)
                lista = lista.Concat(fichasExtra).OrderBy(p => p.Descripcion, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        // 3) Marca las fichas del listado con su cantidad de variantes habilitadas (2 consultas en total)
        var idsLista = lista.Select(p => p.Id).ToList();
        var fichas = await _db.ProductosPrincipales.AsNoTracking()
            .Where(f => idsLista.Contains(f.FkProducto)).Select(f => f.FkProducto).ToListAsync();
        if (fichas.Count > 0)
        {
            var habilitadas = await _db.ProductosVariantes.AsNoTracking()
                .Where(v => idsLista.Contains(v.FkPrincipal) && v.Activa)
                .GroupBy(v => v.FkPrincipal)
                .Select(g => new { Id = g.Key, N = g.Count() })
                .ToListAsync();
            foreach (var f in fichas)
                res.VariantesPorFicha[f] = habilitadas.FirstOrDefault(h => h.Id == f)?.N ?? 0;
        }

        res.Productos = lista;
        return res;
    }

    // ═════════════════════════════════════════════════════════════
    // Lectura de una ficha
    // ═════════════════════════════════════════════════════════════
    public async Task<FichaVariantesDto> ObtenerFichaAsync(int productoId)
    {
        var ficha = await _db.ProductosPrincipales.AsNoTracking().FirstOrDefaultAsync(x => x.FkProducto == productoId);
        if (ficha is null) return new FichaVariantesDto { ProductoId = productoId, TieneVariantes = false };

        var comunCosto  = await _db.CostosProductos.Where(c => c.FkProducto == productoId).Select(c => c.Costo).FirstOrDefaultAsync();
        var comunPrecio = await _db.PreciosProductos.Where(c => c.FkProducto == productoId).Select(c => c.Precio).FirstOrDefaultAsync();

        var medidas = await _db.VariantesMedidas.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Nombre);
        var colores = await _db.VariantesColores.AsNoTracking().ToDictionaryAsync(c => c.Id, c => new { c.Nombre, c.CodigoHex });

        var filas = await (
            from v in _db.ProductosVariantes.AsNoTracking()
            where v.FkPrincipal == productoId
            join p  in _db.Productos         on v.FkProducto equals p.Id
            join s  in _db.StockProductos    on v.FkProducto equals s.FkProducto  into sG  from s  in sG.DefaultIfEmpty()
            join c  in _db.CostosProductos   on v.FkProducto equals c.FkProducto  into cG  from c  in cG.DefaultIfEmpty()
            join pr in _db.PreciosProductos  on v.FkProducto equals pr.FkProducto into prG from pr in prG.DefaultIfEmpty()
            orderby v.Orden, v.FkProducto
            select new
            {
                v.FkProducto, v.FkMedida, v.FkColor, v.Activa, v.Orden,
                p.CodBarras, p.CodProveedor, p.Descripcion,
                Stock  = s  != null ? (s.Cantidad ?? 0m) : 0m,
                Costo  = c  != null ? c.Costo   : null,
                Precio = pr != null ? pr.Precio : null
            })
            .ToListAsync();

        return new FichaVariantesDto
        {
            ProductoId     = productoId,
            TieneVariantes = true,
            UsaMedidas     = ficha.UsaMedidas,
            UsaColores     = ficha.UsaColores,
            PreciosIguales = ficha.PreciosIguales,
            Variantes = filas.Select(f => new VarianteFilaDto
            {
                ProductoId   = f.FkProducto,
                MedidaId     = f.FkMedida,
                ColorId      = f.FkColor,
                MedidaNombre = medidas.GetValueOrDefault(f.FkMedida),
                ColorNombre  = colores.TryGetValue(f.FkColor, out var col) ? col.Nombre : null,
                ColorHex     = colores.TryGetValue(f.FkColor, out var col2) ? col2.CodigoHex : null,
                Habilitada   = f.Activa,
                CodBarras    = f.CodBarras,
                CodProveedor = f.CodProveedor,
                Costo        = f.Costo,
                Precio       = f.Precio,
                Stock        = f.Stock,
                Orden        = f.Orden,
                Descripcion  = f.Descripcion,
                PrecioPropio = ficha.PreciosIguales && (f.Costo != comunCosto || f.Precio != comunPrecio)
            }).ToList()
        };
    }

    public async Task<VistaPreviaPreciosDto> VistaPreviaPrecioComunAsync(int productoId, decimal nuevoCosto, decimal nuevoPrecio)
    {
        var viejoCosto  = await _db.CostosProductos.Where(c => c.FkProducto == productoId).Select(c => c.Costo).FirstOrDefaultAsync();
        var viejoPrecio = await _db.PreciosProductos.Where(c => c.FkProducto == productoId).Select(c => c.Precio).FirstOrDefaultAsync();

        var filas = await (
            from v in _db.ProductosVariantes.AsNoTracking()
            where v.FkPrincipal == productoId
            join p  in _db.Productos        on v.FkProducto equals p.Id
            join c  in _db.CostosProductos  on v.FkProducto equals c.FkProducto  into cG  from c  in cG.DefaultIfEmpty()
            join pr in _db.PreciosProductos on v.FkProducto equals pr.FkProducto into prG from pr in prG.DefaultIfEmpty()
            select new
            {
                p.Descripcion,
                Costo  = c  != null ? c.Costo   : null,
                Precio = pr != null ? pr.Precio : null
            })
            .ToListAsync();

        var vista = new VistaPreviaPreciosDto();
        foreach (var f in filas)
        {
            // "Sigue al común" = su costo y su precio actuales son iguales a los comunes ANTERIORES.
            var sigue = viejoCosto.HasValue && viejoPrecio.HasValue && f.Costo == viejoCosto && f.Precio == viejoPrecio;
            if (sigue) vista.Siguen++;
            else
            {
                vista.ConPrecioPropio++;
                if (vista.Propias.Count < 20) vista.Propias.Add(f.Descripcion ?? "(sin descripción)");
            }
        }
        return vista;
    }

    // ═════════════════════════════════════════════════════════════
    // Guardar ficha + variantes
    // ═════════════════════════════════════════════════════════════
    public async Task<ResultadoVariantes> GuardarFichaAsync(ProductoDetallesDTO p, GuardarVariantesRequest req)
    {
        if (!await HabilitadoAsync())
            return ResultadoVariantes.Fallo(new[] { "Las variantes de producto no están habilitadas en esta base." });

        var pid        = p.Id ?? 0;
        var errores    = new List<string>();
        var filas      = req.Variantes ?? new List<VarianteFilaDto>();
        var nombreBase = (p.Descripcion ?? string.Empty).Trim();
        var precioComun = p.Precio ?? 0m;
        var costoComun  = p.Costo  ?? 0m;

        if (nombreBase.Length == 0) errores.Add("Escribí la descripción del producto.");
        // El detalle de producto del ABM exige rubro y proveedor (también para las variantes, que los heredan).
        if (!p.FkRubro.HasValue)     errores.Add("Seleccioná el rubro.");
        if (!p.FkProveedor.HasValue) errores.Add("Seleccioná el proveedor.");
        if (!req.UsaMedidas && !req.UsaColores) errores.Add("Elegí si el producto varía por medidas, colores o ambos.");
        if (filas.Count == 0) errores.Add("Agregá al menos una variante.");

        // ── Estado actual de la ficha ────────────────────────────────────────
        ProductoPrincipal? principal = null;
        var existentes = new List<ProductoVariante>();
        if (pid > 0)
        {
            principal = await _db.ProductosPrincipales.FirstOrDefaultAsync(x => x.FkProducto == pid);
            if (principal is null)
                errores.Add("Este producto ya existe como producto simple. Para usar variantes, creá un producto nuevo.");
            else
                existentes = await _db.ProductosVariantes.Where(v => v.FkPrincipal == pid).ToListAsync();
        }

        // Productos, stock, precios y costos de las variantes existentes (con seguimiento para poder modificarlos)
        var idsExistentes = existentes.Select(v => v.FkProducto).ToList();
        var prodsEx   = await _db.Productos.Where(x => idsExistentes.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
        var costosEx  = (await _db.CostosProductos.Where(x => x.FkProducto != null && idsExistentes.Contains(x.FkProducto.Value)).ToListAsync())
                            .GroupBy(x => x.FkProducto!.Value).ToDictionary(g => g.Key, g => g.First());
        var preciosEx = (await _db.PreciosProductos.Where(x => x.FkProducto != null && idsExistentes.Contains(x.FkProducto.Value)).ToListAsync())
                            .GroupBy(x => x.FkProducto!.Value).ToDictionary(g => g.Key, g => g.First());

        if (principal is not null && existentes.Count > 0 &&
            (principal.UsaMedidas != req.UsaMedidas || principal.UsaColores != req.UsaColores))
            errores.Add("No se pueden cambiar las dimensiones (medidas/colores) de un producto que ya tiene variantes.");

        // ── Atributos del maestro ────────────────────────────────────────────
        var medidas = await _db.VariantesMedidas.AsNoTracking().ToDictionaryAsync(m => m.Id);
        var colores = await _db.VariantesColores.AsNoTracking().ToDictionaryAsync(c => c.Id);

        var idsFamilia = existentes.Select(v => v.FkProducto).ToHashSet();
        var porId      = existentes.ToDictionary(v => v.FkProducto);
        var combosExistentes = existentes.Select(v => (v.FkMedida, v.FkColor)).ToHashSet();
        var combosFilas      = new HashSet<(int, int)>();
        var descripciones    = new Dictionary<VarianteFilaDto, string>();

        foreach (var f in filas)
        {
            if (!req.UsaMedidas) f.MedidaId = 0;
            else if (f.MedidaId <= 0) { errores.Add("Hay una fila sin medida."); continue; }
            if (!req.UsaColores) f.ColorId = 0;
            else if (f.ColorId <= 0) { errores.Add("Hay una fila sin color."); continue; }

            medidas.TryGetValue(f.MedidaId, out var medida);
            colores.TryGetValue(f.ColorId, out var color);
            var etiqueta = Etiqueta(medida?.Nombre, color?.Nombre);

            if (req.UsaMedidas && medida is null) { errores.Add($"La medida de la fila '{etiqueta}' no existe."); continue; }
            if (req.UsaColores && color  is null) { errores.Add($"El color de la fila '{etiqueta}' no existe."); continue; }

            if (!combosFilas.Add((f.MedidaId, f.ColorId)))
                errores.Add($"La combinación {etiqueta} está repetida.");

            if (f.ProductoId.HasValue)
            {
                if (!porId.TryGetValue(f.ProductoId.Value, out var ex))
                    errores.Add($"La variante {etiqueta} no pertenece a este producto.");
                else if (ex.FkMedida != f.MedidaId || ex.FkColor != f.ColorId)
                    errores.Add($"No se puede cambiar la medida o el color de la variante existente {etiqueta}.");
            }
            else
            {
                if (combosExistentes.Contains((f.MedidaId, f.ColorId)))
                    errores.Add($"La combinación {etiqueta} ya existe en este producto.");
                if (f.Habilitada && medida is { Activa: false }) errores.Add($"La medida {medida.Nombre} está deshabilitada.");
                if (f.Habilitada && color  is { Activa: false }) errores.Add($"El color {color.Nombre} está deshabilitado.");
            }

            // Descripción generada: cabe en Productos.descripcion y es compatible con latin1.
            var desc = Generar(nombreBase, req.UsaMedidas, req.UsaColores, medida?.Nombre, color?.Nombre);
            if (desc.Length > MaxDescripcion)
                errores.Add($"La descripción '{desc}' supera los {MaxDescripcion} caracteres: acortá el nombre del producto, la medida o el color.");
            else if (!EsLatin1(desc))
                errores.Add($"La descripción '{desc}' tiene caracteres que la base no admite (por ejemplo emojis).");
            descripciones[f] = desc;

            // Códigos
            f.CodBarras    = Limpiar(f.CodBarras);
            f.CodProveedor = Limpiar(f.CodProveedor);
            if (f.CodBarras?.Length    > MaxCodigo) errores.Add($"El código de barras de {etiqueta} admite hasta {MaxCodigo} caracteres.");
            if (f.CodProveedor?.Length > MaxCodigo) errores.Add($"El código de proveedor de {etiqueta} admite hasta {MaxCodigo} caracteres.");

            // Stock inicial: entero, solo variantes nuevas
            if (f.StockInicial < 0 || f.StockInicial != decimal.Truncate(f.StockInicial))
                errores.Add($"El stock inicial de {etiqueta} debe ser un número entero mayor o igual a cero.");
            if (f.ProductoId.HasValue) f.StockInicial = 0;

            if ((f.Costo ?? 0) < 0 || (f.Precio ?? 0) < 0)
                errores.Add($"El costo y el precio de {etiqueta} no pueden ser negativos.");
        }

        // ── Códigos repetidos: dentro de la grilla y contra el resto de los productos ─────
        var idsPropios = new HashSet<int>(idsFamilia);
        if (pid > 0) idsPropios.Add(pid);

        foreach (var g in filas.Where(f => TieneCodigoDeBarras(f.CodBarras))
                               .GroupBy(f => f.CodBarras!.ToLowerInvariant()).Where(g => g.Count() > 1))
            errores.Add($"El código de barras {g.First().CodBarras} está repetido en la grilla.");
        foreach (var g in filas.Where(f => f.CodProveedor is not null)
                               .GroupBy(f => f.CodProveedor!.ToLowerInvariant()).Where(g => g.Count() > 1))
            errores.Add($"El código de proveedor {g.First().CodProveedor} está repetido en la grilla.");

        var listaBarras = filas.Where(f => TieneCodigoDeBarras(f.CodBarras)).Select(f => f.CodBarras!).Distinct().ToList();
        if (listaBarras.Count > 0)
        {
            // Los marcadores vacíos ("", "0", "000") no cuentan: en la base hay decenas repetidos.
            var choques = await _db.Productos.AsNoTracking()
                .Where(x => x.CodBarras != null && listaBarras.Contains(x.CodBarras) && x.Baja != true && !idsPropios.Contains(x.Id))
                .Select(x => new { x.CodBarras, x.Descripcion })
                .Take(10).ToListAsync();
            foreach (var c in choques)
                errores.Add($"El código de barras {c.CodBarras} ya lo usa el producto '{c.Descripcion}'.");
        }

        var listaProv = filas.Where(f => f.CodProveedor is not null).Select(f => f.CodProveedor!).Distinct().ToList();
        if (listaProv.Count > 0)
        {
            var fkProveedor = p.FkProveedor;
            var choques = await _db.Productos.AsNoTracking()
                .Where(x => x.CodProveedor != null && listaProv.Contains(x.CodProveedor) && x.Baja != true
                            && x.FkProveedor == fkProveedor && !idsPropios.Contains(x.Id))
                .Select(x => new { x.CodProveedor, x.Descripcion })
                .Take(10).ToListAsync();
            foreach (var c in choques)
                errores.Add($"El código de proveedor {c.CodProveedor} ya lo usa el producto '{c.Descripcion}' (mismo proveedor).");
        }

        // ── Precios ──────────────────────────────────────────────────────────
        if (req.PreciosIguales)
        {
            if (precioComun <= 0) errores.Add("Cargá el precio de venta común (mayor a cero).");
            if (costoComun < 0)   errores.Add("El costo común no puede ser negativo.");
        }

        // Costo/precio efectivo de cada fila (nueva: propio o común; existente: propio o el actual)
        var efectivos = new Dictionary<VarianteFilaDto, (decimal Costo, decimal Precio)>();
        foreach (var f in filas)
        {
            var idFila = f.ProductoId ?? -1;
            costosEx.TryGetValue(idFila, out var cEx);
            preciosEx.TryGetValue(idFila, out var pEx);
            var costo  = f.Costo  ?? cEx?.Costo   ?? (req.PreciosIguales ? costoComun  : 0m);
            var precio = f.Precio ?? pEx?.Precio  ?? (req.PreciosIguales ? precioComun : 0m);
            efectivos[f] = (costo, precio);

            if (f.ProductoId is null && f.Habilitada && precio <= 0)
                errores.Add($"Falta el precio de venta de {Etiqueta(f.MedidaNombre ?? medidas.GetValueOrDefault(f.MedidaId)?.Nombre,
                                                                    f.ColorNombre  ?? colores.GetValueOrDefault(f.ColorId)?.Nombre)}.");
        }

        if (errores.Count > 0)
            return ResultadoVariantes.Fallo(errores.Distinct());

        // ── Valores comunes anteriores (para decidir qué variantes "siguen" al cambiar el común) ──
        decimal? viejoCosto = null, viejoPrecio = null;
        var bajaPrincipal = false;
        if (principal is not null)
        {
            viejoCosto  = await _db.CostosProductos.Where(c => c.FkProducto == pid).Select(c => c.Costo).FirstOrDefaultAsync();
            viejoPrecio = await _db.PreciosProductos.Where(c => c.FkProducto == pid).Select(c => c.Precio).FirstOrDefaultAsync();
            bajaPrincipal = await _db.Productos.Where(x => x.Id == pid).Select(x => x.Baja).FirstOrDefaultAsync() == true;
        }

        // Sin precios iguales, la ficha guarda como referencia el menor costo/precio de las variantes habilitadas.
        if (!req.PreciosIguales)
        {
            var habilitadas = filas.Where(f => f.Habilitada).Select(f => efectivos[f]).ToList();
            p.Precio = habilitadas.Count > 0 ? habilitadas.Min(e => e.Precio) : 0m;
            p.Costo  = habilitadas.Count > 0 ? habilitadas.Min(e => e.Costo)  : 0m;
            precioComun = p.Precio.Value;
            costoComun  = p.Costo.Value;
        }
        // El stock es de cada variante: la ficha nunca tiene stock propio.
        p.Cantidad = 0;

        var resultado = new ResultadoVariantes();
        var nuevas = new List<(Producto Prod, VarianteFilaDto Fila, int Orden)>();

        // Código de barras automático para las variantes NUEVAS que vengan sin ninguno de los dos códigos.
        // La ficha principal nunca recibe código. El parámetro se consulta antes de abrir la transacción.
        var sinCodigo = filas.Where(f => f.ProductoId is null && f.CodBarras is null && f.CodProveedor is null).ToList();
        if (sinCodigo.Count > 0 && !await _codigos.HabilitadoAsync()) sinCodigo.Clear();

        // Si quien llama ya abrió una transacción, se une a ella (y es quien confirma o revierte).
        var propia = _db.Database.CurrentTransaction == null;
        await using var tx = propia ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            // Primera lectura de la transacción: se reservan TODOS los códigos de la operación de una sola vez
            // (excluyendo los que el usuario escribió en otras filas o en la ficha).
            if (sinCodigo.Count > 0)
            {
                var escritos = filas.SelectMany(f => new[] { f.CodBarras, f.CodProveedor })
                    .Append(p.CodBarras).Append(p.CodProveedor)
                    .Where(c => c != null).Select(c => c!);
                var codigos = await _codigos.ReservarAsync(sinCodigo.Count, escritos);
                for (var i = 0; i < sinCodigo.Count; i++)
                    sinCodigo[i].CodBarras = sinCodigo[i].CodProveedor = codigos[i];
                resultado.CodigosGenerados = sinCodigo.Count;
            }

            // 1) Ficha principal (producto + stock/precio/costo/proveedor + imágenes) con el servicio existente,
            //    dentro de esta misma transacción (ProductoService se une a la transacción en curso).
            if (principal is null) await _productos.CrearAsync(p, generarCodigo: false);   // la ficha no lleva código
            else                   await _productos.ActualizarAsync(p);
            pid = p.Id ?? throw new InvalidOperationException("No se pudo obtener el id del producto creado.");
            resultado.ProductoId = pid;

            if (principal is null)
            {
                principal = new ProductoPrincipal { FkProducto = pid };
                _db.ProductosPrincipales.Add(principal);
            }
            principal.UsaMedidas     = req.UsaMedidas;
            principal.UsaColores     = req.UsaColores;
            principal.PreciosIguales = req.PreciosIguales;
            await _db.SaveChangesAsync();

            // Atributos comunes que heredan las variantes (se leen de la ficha ya guardada)
            var ficha = await _db.Productos.FirstAsync(x => x.Id == pid);

            // 2) Variantes del pedido
            var enviadas = new HashSet<int>();
            var orden = 0;
            foreach (var f in filas)
            {
                orden++;
                var desc = descripciones[f];
                var (costo, precio) = efectivos[f];

                if (f.ProductoId is null)
                {
                    var prod = new Producto
                    {
                        CodProveedor = f.CodProveedor, CodBarras = f.CodBarras, Descripcion = desc,
                        FkRubro = ficha.FkRubro, FkProveedor = ficha.FkProveedor, Iva = ficha.Iva,
                        EsPromocion = ficha.EsPromocion, Fraccionado = ficha.Fraccionado, Dolarizado = ficha.Dolarizado,
                        Ganancia = ficha.Ganancia, Descuento = ficha.Descuento,
                        CantidadMinimaVenta = ficha.CantidadMinimaVenta, DescripcionLarga = ficha.DescripcionLarga,
                        Baja = bajaPrincipal || !f.Habilitada
                    };
                    _db.Productos.Add(prod);
                    nuevas.Add((prod, f, f.Orden > 0 ? f.Orden : orden));
                    resultado.Creadas++;
                    continue;
                }

                var ex     = porId[f.ProductoId.Value];
                var prodEx = prodsEx[ex.FkProducto];
                enviadas.Add(ex.FkProducto);

                prodEx.CodBarras    = f.CodBarras;
                prodEx.CodProveedor = f.CodProveedor;
                prodEx.Descripcion  = desc;

                if (f.Habilitada != ex.Activa)
                {
                    ex.Activa = f.Habilitada;
                    if (f.Habilitada) resultado.Habilitadas++; else resultado.Deshabilitadas++;
                }
                if (f.Orden > 0) ex.Orden = f.Orden;

                // Precios de una variante existente: valor propio de la fila, o la regla de propagación del común.
                costosEx.TryGetValue(ex.FkProducto, out var cEx);
                preciosEx.TryGetValue(ex.FkProducto, out var pEx);
                var costoActual  = cEx?.Costo;
                var precioActual = pEx?.Precio;
                var nuevoCosto   = costoActual  ?? 0m;
                var nuevoPrecio  = precioActual ?? 0m;
                var propagado    = false;

                if (f.Costo.HasValue) nuevoCosto = f.Costo.Value;
                else if (req.PreciosIguales && DebePropagar(req.ModoPropagacion, viejoCosto, costoActual))
                { nuevoCosto = costoComun; propagado |= nuevoCosto != costoActual; }

                if (f.Precio.HasValue) nuevoPrecio = f.Precio.Value;
                else if (req.PreciosIguales && DebePropagar(req.ModoPropagacion, viejoPrecio, precioActual))
                { nuevoPrecio = precioComun; propagado |= nuevoPrecio != precioActual; }

                if (propagado) resultado.PreciosPropagados++;
                if (req.PreciosIguales && !f.Costo.HasValue && !f.Precio.HasValue
                    && (nuevoCosto != costoComun || nuevoPrecio != precioComun))
                    resultado.PreciosOmitidos++;   // quedó con algún valor propio (distinto del común)

                if (cEx is null) _db.CostosProductos.Add(new CostosProducto  { FkProducto = ex.FkProducto, Costo  = nuevoCosto });  else cEx.Costo  = nuevoCosto;
                if (pEx is null) _db.PreciosProductos.Add(new PreciosProducto { FkProducto = ex.FkProducto, Precio = nuevoPrecio }); else pEx.Precio = nuevoPrecio;
                resultado.Actualizadas++;   // la baja de cada variante se resuelve abajo, junto con los atributos comunes
            }

            // 3) Atributos comunes y baja en TODAS las variantes existentes (también las que no vinieron en el pedido)
            foreach (var ex in existentes)
            {
                var prodEx = prodsEx[ex.FkProducto];
                prodEx.FkRubro             = ficha.FkRubro;
                prodEx.FkProveedor         = ficha.FkProveedor;
                prodEx.Iva                 = ficha.Iva;
                prodEx.EsPromocion         = ficha.EsPromocion;
                prodEx.Fraccionado         = ficha.Fraccionado;
                prodEx.Dolarizado          = ficha.Dolarizado;
                prodEx.Ganancia            = ficha.Ganancia;
                prodEx.Descuento           = ficha.Descuento;
                prodEx.CantidadMinimaVenta = ficha.CantidadMinimaVenta;
                prodEx.DescripcionLarga    = ficha.DescripcionLarga;
                prodEx.Baja                = bajaPrincipal || !ex.Activa;

                if (!enviadas.Contains(ex.FkProducto))
                {
                    var desc = Generar(nombreBase, req.UsaMedidas, req.UsaColores,
                        medidas.GetValueOrDefault(ex.FkMedida)?.Nombre, colores.GetValueOrDefault(ex.FkColor)?.Nombre);
                    if (desc.Length <= MaxDescripcion && EsLatin1(desc)) prodEx.Descripcion = desc;
                }
            }

            // Stock mínimo común para las variantes existentes (el stock actual no se toca)
            var stocksEx = await _db.StockProductos.Where(s => s.FkProducto != null && idsExistentes.Contains(s.FkProducto.Value)).ToListAsync();
            foreach (var s in stocksEx) s.CantidadMinima = p.CantidadMinima;

            await _db.SaveChangesAsync();   // inserta los productos nuevos (obtiene sus ids)

            // 4) Dependientes de cada variante nueva: enlace a la ficha, stock, precio, costo y precio de proveedor
            //    (el detalle de producto del ABM necesita todas esas filas para abrirse).
            foreach (var (prod, fila, ordenFila) in nuevas)
            {
                var (costo, precio) = efectivos[fila];
                _db.ProductosVariantes.Add(new ProductoVariante
                {
                    FkProducto = prod.Id, FkPrincipal = pid, FkMedida = fila.MedidaId, FkColor = fila.ColorId,
                    Activa = fila.Habilitada, Orden = ordenFila
                });
                _db.StockProductos.Add(new StockProducto
                {
                    FkProducto = prod.Id, Cantidad = fila.StockInicial, CantidadMinima = p.CantidadMinima
                });
                _db.PreciosProductos.Add(new PreciosProducto  { FkProducto = prod.Id, Precio = precio });
                _db.CostosProductos.Add(new CostosProducto    { FkProducto = prod.Id, Costo  = costo });
                _db.PreciosProveedores.Add(new PreciosProveedore { FkProducto = prod.Id, Precio = costo });
            }
            if (nuevas.Count > 0) await _db.SaveChangesAsync();

            if (propia) await tx!.CommitAsync();
        }
        catch
        {
            if (propia) await tx!.RollbackAsync();
            throw;
        }

        resultado.Ok = true;
        return resultado;
    }

    // ═════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════

    /// <summary>
    /// ¿Se le aplica a la variante el nuevo valor común? Modo "todas": sí. Modo "solo quienes siguen": solo si su
    /// valor actual es igual al común anterior. Modo "ninguna": no.
    /// </summary>
    private static bool DebePropagar(int modo, decimal? comunAnterior, decimal? valorActual) => modo switch
    {
        ModoPropagacionPrecio.Todas             => true,
        ModoPropagacionPrecio.SoloQuienesSiguen => comunAnterior.HasValue && valorActual == comunAnterior,
        _                                       => false
    };

    /// <summary>"NOMBRE BASE - MEDIDA - COLOR" (solo las dimensiones que usa la ficha).</summary>
    internal static string Generar(string nombreBase, bool usaMedidas, bool usaColores, string? medida, string? color)
    {
        var partes = new List<string> { nombreBase.Trim() };
        if (usaMedidas && !string.IsNullOrWhiteSpace(medida)) partes.Add(medida.Trim());
        if (usaColores && !string.IsNullOrWhiteSpace(color))  partes.Add(color.Trim());
        return string.Join(" - ", partes);
    }

    private static string Etiqueta(string? medida, string? color) =>
        string.Join(" / ", new[] { medida, color }.Where(s => !string.IsNullOrWhiteSpace(s)).DefaultIfEmpty("(sin atributos)"));

    private static string? Limpiar(string? s)
    {
        s = s?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    /// <summary>Un código de barras "real": ni vacío ni solo ceros (hay decenas de "0" repetidos en la base).</summary>
    internal static bool TieneCodigoDeBarras(string? codigo) =>
        !string.IsNullOrWhiteSpace(codigo) && codigo.Any(c => c != '0');

    /// <summary>La base de productos es latin1: solo se admiten caracteres hasta U+00FF.</summary>
    internal static bool EsLatin1(string s) => s.All(c => c <= 'ÿ');
}
