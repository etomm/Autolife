using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IKnowledgeService
{
    Task<List<KnowledgeEntry>> GetAllAsync();
    Task<KnowledgeEntry?> GetByIdAsync(Guid id);
    Task<KnowledgeEntry> CreateAsync(KnowledgeEntry entry);
    Task<KnowledgeEntry> UpdateAsync(KnowledgeEntry entry);
    Task DeleteAsync(Guid id);
    Task<List<KnowledgeEntry>> SearchAsync(string query);
}
