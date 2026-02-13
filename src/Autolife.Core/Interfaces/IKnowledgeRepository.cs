using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IKnowledgeRepository
{
    Task<KnowledgeEntry?> GetByIdAsync(Guid id);
    Task<IEnumerable<KnowledgeEntry>> GetAllAsync();
    Task<IEnumerable<KnowledgeEntry>> SearchAsync(string query);
    Task<IEnumerable<KnowledgeEntry>> GetByCategoryAsync(string category);
    Task<IEnumerable<KnowledgeEntry>> GetByTagsAsync(List<string> tags);
    Task<KnowledgeEntry> CreateAsync(KnowledgeEntry entry);
    Task<KnowledgeEntry> UpdateAsync(KnowledgeEntry entry);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<string>> GetAllCategoriesAsync();
    Task<IEnumerable<string>> GetAllTagsAsync();
}
