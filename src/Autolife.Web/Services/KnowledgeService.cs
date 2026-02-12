using Autolife.Core.Interfaces;
using Autolife.Core.Models;
using Autolife.Storage;

namespace Autolife.Web.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly GitStorageService _storage;

    public KnowledgeService(GitStorageService storage)
    {
        _storage = storage;
    }

    public async Task<List<KnowledgeEntry>> GetAllAsync()
    {
        return await _storage.LoadAllKnowledgeAsync();
    }

    public async Task<KnowledgeEntry?> GetByIdAsync(Guid id)
    {
        return await _storage.LoadKnowledgeAsync(id);
    }

    public async Task<KnowledgeEntry> CreateAsync(KnowledgeEntry entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveKnowledgeAsync(entry);
        return entry;
    }

    public async Task<KnowledgeEntry> UpdateAsync(KnowledgeEntry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveKnowledgeAsync(entry);
        return entry;
    }

    public async Task DeleteAsync(Guid id)
    {
        // In git-based storage, we could implement soft delete by moving to archive
        // For now, this is a placeholder
        await Task.CompletedTask;
    }

    public async Task<List<KnowledgeEntry>> SearchAsync(string query)
    {
        var all = await GetAllAsync();
        var lowerQuery = query.ToLower();
        
        return all.Where(k => 
            k.Title.ToLower().Contains(lowerQuery) ||
            k.Content.ToLower().Contains(lowerQuery) ||
            k.Tags.Any(t => t.ToLower().Contains(lowerQuery)) ||
            k.Category.ToLower().Contains(lowerQuery)
        ).ToList();
    }
}
