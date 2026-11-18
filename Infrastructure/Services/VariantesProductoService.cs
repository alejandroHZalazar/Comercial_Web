using Domain.Contracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class VariantesProductoService : IVariantesProductoService
{
    private const string Modulo    = "ecommerce";
    private const string Parametro = "HabilitarVariantesProducto";
    private static readonly TimeSpan VigenciaCache = TimeSpan.FromSeconds(60);

    // Estado compartido por todo el proceso: evita consultar el parámetro en cada búsqueda de productos.
    private static (bool Habilitado, string? Tabla, DateTime Vence) _estado = (false, null, DateTime.MinValue);

    private readonly ComercialDbContext _db;
    public VariantesProductoService(ComercialDbContext db) => _db = db;

    public async Task<bool> HabilitadoAsync() => (await ObtenerEstadoAsync()).Habilitado;

    public async Task<HashSet<int>> FichasPrincipalesAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0) return new HashSet<int>();

        var (habilitado, tabla) = await ObtenerEstadoAsync();
        if (!habilitado || tabla == null) return new HashSet<int>();

        // Los ids son enteros y el nombre de la tabla proviene de information_schema: no hay datos de usuario en el SQL.
        var sql = $"SELECT fk_producto AS `Value` FROM `{tabla.Replace("`", "``")}` WHERE fk_producto IN ({string.Join(",", ids)})";
        return (await _db.Database.SqlQueryRaw<int>(sql).ToListAsync()).ToHashSet();
    }

    public async Task ValidarNoSonFichasAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.ToList();
        var fichas = await FichasPrincipalesAsync(ids);
        if (fichas.Count == 0) return;

        var nombres = await _db.Productos.AsNoTracking()
            .Where(p => fichas.Contains(p.Id))
            .Select(p => p.Descripcion)
            .ToListAsync();

        throw new InvalidOperationException(
            "No se pueden vender ni pedir fichas principales: " +
            string.Join(", ", nombres.Select(n => $"'{n}'")) +
            ". Seleccione una de sus variantes (medida/color).");
    }

    // Habilitado = parámetro en '1' Y tabla productosPrincipales existente. Con el parámetro apagado
    // (o ausente) no se consulta information_schema ni ninguna tabla de variantes.
    private async Task<(bool Habilitado, string? Tabla)> ObtenerEstadoAsync()
    {
        var actual = _estado;
        if (actual.Vence > DateTime.UtcNow) return (actual.Habilitado, actual.Tabla);

        var valor = await _db.Parametros.AsNoTracking()
            .Where(p => p.Modulo == Modulo && p.Parametro1 == Parametro)
            .Select(p => p.Valor)
            .FirstOrDefaultAsync();

        var habilitado = false;
        string? tabla  = null;
        if (valor?.Trim() == "1")
        {
            // El nombre exacto se toma de la base (en Linux MySQL distingue mayúsculas en nombres de tabla).
            tabla = (await _db.Database.SqlQueryRaw<string>(
                    "SELECT TABLE_NAME AS `Value` FROM information_schema.TABLES " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = 'productosprincipales'")
                .ToListAsync()).FirstOrDefault();
            habilitado = tabla != null;
        }

        _estado = (habilitado, tabla, DateTime.UtcNow.Add(VigenciaCache));
        return (habilitado, tabla);
    }
}
