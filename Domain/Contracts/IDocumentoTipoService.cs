using Domain.Entities;

namespace Domain.Contracts
{
    public interface IDocumentoTipoService
    {
        Task<List<DocumentoTipo>> GetAllAsync();
        Task<DocumentoTipo?> GetByIdAsync(int id);
        Task<DocumentoTipo> CreateAsync(string abreviatura, string? descripcion);
        Task UpdateAsync(int id, string abreviatura, string? descripcion);
        Task DeleteAsync(int id);
    }
}
