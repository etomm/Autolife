using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IDocumentService
{
    Task<List<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(Guid id);
    Task<Document> CreateAsync(Document document, Stream fileStream);
    Task<Document> UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task<List<Document>> GetByProjectIdAsync(Guid projectId);
    Task<Stream> GetFileStreamAsync(Guid id);
}
