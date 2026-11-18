namespace Infrastructure.Services;

/// <summary>
/// Hora local de Argentina (UTC-3, sin horario de verano), independiente de la zona del servidor
/// (en producción Linux/Docker el reloj es UTC). La usan las promociones por cantidad/porcentaje:
/// su vigencia se guarda y compara siempre en hora argentina.
/// </summary>
public static class HoraArgentina
{
    private static readonly TimeZoneInfo? Zona = Buscar();

    public static DateTime Ahora()
        => Zona != null
            ? TimeZoneInfo.ConvertTime(DateTime.UtcNow, Zona)
            : DateTime.UtcNow.AddHours(-3);

    private static TimeZoneInfo? Buscar()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* probar el siguiente id */ }
        }
        return null;   // sin datos de zona (p. ej. contenedor sin tzdata): se usa UTC-3 fijo
    }
}
