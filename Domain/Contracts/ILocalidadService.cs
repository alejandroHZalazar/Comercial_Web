using Domain.Entities;

public interface ILocalidadService
{
    Task<List<Provincia>> GetProvinciasAsync();
    Task<List<Localidade>> GetByProvinciaAsync(int? provinciaId);
    Task<Localidade?> GetByIdAsync(int id);
    Task CreateAsync(Localidade loc);
    Task UpdateAsync(Localidade loc);
    Task DeleteAsync(int id);
    Task<List<Localidade>> GetAllAsync();
}
