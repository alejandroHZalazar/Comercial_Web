namespace Domain.DTO;

public class RubroNodoDto
{
    public int     Id           { get; set; }
    public string  Descripcion  { get; set; } = string.Empty;
    public int?    FkRubroPadre { get; set; }

    /// <summary>Ruta completa, ej. "Bebidas &gt; Gaseosas &gt; SUBABY SRL".</summary>
    public string  Ruta         { get; set; } = string.Empty;

    /// <summary>0 = rubro principal.</summary>
    public int     Nivel        { get; set; }
}
