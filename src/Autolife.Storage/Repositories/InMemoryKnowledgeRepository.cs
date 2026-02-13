using System.Collections.Concurrent;
using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Storage.Repositories;

public class InMemoryKnowledgeRepository : IKnowledgeRepository
{
    private readonly ConcurrentDictionary<Guid, KnowledgeEntry> _entries = new();

    public Task<KnowledgeEntry?> GetByIdAsync(Guid id)
    {
        _entries.TryGetValue(id, out var entry);
        return Task.FromResult(entry);
    }

    public Task<IEnumerable<KnowledgeEntry>> GetAllAsync()
    {
        return Task.FromResult(_entries.Values.Where(e => !e.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<KnowledgeEntry>> SearchAsync(string query)
    {
        var results = _entries.Values
            .Where(e => !e.IsDeleted && (
                e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            ));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<KnowledgeEntry>> GetByCategoryAsync(string category)
    {
        var results = _entries.Values
            .Where(e => !e.IsDeleted && e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<IEnumerable<KnowledgeEntry>> GetByTagsAsync(List<string> tags)
    {
        var results = _entries.Values
            .Where(e => !e.IsDeleted && e.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<KnowledgeEntry> CreateAsync(KnowledgeEntry entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        _entries.TryAdd(entry.Id, entry);
        return Task.FromResult(entry);
    }

    public Task<KnowledgeEntry> UpdateAsync(KnowledgeEntry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        _entries[entry.Id] = entry;
        return Task.FromResult(entry);
    }

    public Task DeleteAsync(Guid id)
    {
        if (_entries.TryGetValue(id, out var entry))
        {
            entry.IsDeleted = true;
            entry.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        var categories = _entries.Values
            .Where(e => !e.IsDeleted)
            .Select(e => e.Category)
            .Distinct()
            .Where(c => !string.IsNullOrWhiteSpace(c));
        return Task.FromResult(categories.AsEnumerable());
    }

    public Task<IEnumerable<string>> GetAllTagsAsync()
    {
        var tags = _entries.Values
            .Where(e => !e.IsDeleted)
            .SelectMany(e => e.Tags)
            .Distinct()
            .Where(t => !string.IsNullOrWhiteSpace(t));
        return Task.FromResult(tags.AsEnumerable());
    }
}
