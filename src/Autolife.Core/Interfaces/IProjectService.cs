using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IProjectService
{
    Task<List<Project>> GetAllAsync();
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<Project> CreateFromKnowledgeAsync(Guid knowledgeId, string projectTitle);
}
