using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task<IEnumerable<Document>> SearchAsync(string query);
    Task<IEnumerable<Document>> GetByCategoryAsync(string category);
    Task<IEnumerable<Document>> GetByProjectIdAsync(Guid projectId);
    Task<Document> CreateAsync(Document document);
    Task<Document> UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
}
