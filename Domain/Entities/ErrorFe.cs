namespace Domain.Entities;
public class ErrorFe
{
    public int     Id       { get; set; }
    public int?    FkVenta  { get; set; }
    public string? Error    { get; set; }
}
