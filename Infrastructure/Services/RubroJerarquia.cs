using Domain.DTO;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Utilidades para la jerarquía de rubros (Rubros.fk_rubroPadre). Se resuelve en memoria porque
/// la cantidad de rubros es chica, sin depender de la tabla RubrosArbol ni de la vista.
/// </summary>
internal static class RubroJerarquia
{
    public const string Separador = " > ";

    public static async Task<List<RubroNodoDto>> CargarAsync(ComercialDbContext db)
    {
        var rubros = await db.Rubros.AsNoTracking()
            .Select(r => new { r.Id, r.Descripcion, r.FkRubroPadre })
            .ToListAsync();
        var porId = rubros.ToDictionary(r => r.Id);

        return rubros.Select(r =>
        {
            // La ruta se arma subiendo por los padres; el límite evita un bucle ante datos corruptos.
            var partes = new List<string>();
            var actual = r;
            var guarda = 0;
            while (actual != null && guarda++ < 50)
            {
                partes.Insert(0, (actual.Descripcion ?? string.Empty).Trim());
                actual = actual.FkRubroPadre.HasValue && porId.TryGetValue(actual.FkRubroPadre.Value, out var padre)
                    ? padre : null;
            }

            return new RubroNodoDto
            {
                Id           = r.Id,
                Descripcion  = (r.Descripcion ?? string.Empty).Trim(),
                FkRubroPadre = r.FkRubroPadre,
                Ruta         = string.Join(Separador, partes),
                Nivel        = partes.Count - 1
            };
        })
        .OrderBy(n => n.Ruta, StringComparer.CurrentCultureIgnoreCase)
        .ToList();
    }

    /// <summary>Devuelve los rubros indicados más todos sus descendientes (subrubros de cualquier nivel).</summary>
    public static async Task<List<int>> ExpandirConDescendientesAsync(ComercialDbContext db, IEnumerable<int> ids)
    {
        var resultado = new HashSet<int>(ids);
        if (resultado.Count == 0) return new List<int>();

        var hijosPorPadre = (await db.Rubros.AsNoTracking()
                .Where(r => r.FkRubroPadre != null)
                .Select(r => new { r.Id, Padre = r.FkRubroPadre!.Value })
                .ToListAsync())
            .ToLookup(r => r.Padre, r => r.Id);

        var pendientes = new Queue<int>(resultado);
        while (pendientes.Count > 0)
            foreach (var hijo in hijosPorPadre[pendientes.Dequeue()])
                if (resultado.Add(hijo))
                    pendientes.Enqueue(hijo);

        return resultado.ToList();
    }

    public static async Task<string?> RutaAsync(ComercialDbContext db, int? rubroId)
    {
        if (!rubroId.HasValue) return null;
        return (await CargarAsync(db)).FirstOrDefault(n => n.Id == rubroId.Value)?.Ruta;
    }
}
