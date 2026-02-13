using System.Collections.Concurrent;
using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Storage.Repositories;

public class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public Task<Document?> GetByIdAsync(Guid id)
    {
        _documents.TryGetValue(id, out var doc);
        return Task.FromResult(doc);
    }

    public Task<IEnumerable<Document>> GetAllAsync()
    {
        return Task.FromResult(_documents.Values.Where(d => !d.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<Document>> SearchAsync(string query)
    {
        var results = _documents.Values
            .Where(d => !d.IsDeleted && (
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.OriginalFileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.AiExtractedText.Contains(query, StringComparison.OrdinalIgnoreCase)
            ));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<Document>> GetByCategoryAsync(string category)
    {
        var results = _documents.Values
            .Where(d => !d.IsDeleted && d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<Document>> GetByProjectIdAsync(Guid projectId)
    {
        var results = _documents.Values
            .Where(d => !d.IsDeleted && d.ProjectId == projectId);
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<Document> CreateAsync(Document document)
    {
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;
        _documents.TryAdd(document.Id, document);
        return Task.FromResult(document);
    }

    public Task<Document> UpdateAsync(Document document)
    {
        document.UpdatedAt = DateTime.UtcNow;
        _documents[document.Id] = document;
        return Task.FromResult(document);
    }

    public Task DeleteAsync(Guid id)
    {
        if (_documents.TryGetValue(id, out var doc))
        {
            doc.IsDeleted = true;
            doc.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        var categories = _documents.Values
            .Where(d => !d.IsDeleted)
            .Select(d => d.Category)
            .Distinct()
            .Where(c => !string.IsNullOrWhiteSpace(c));
        return Task.FromResult(categories.AsEnumerable());
    }
}
