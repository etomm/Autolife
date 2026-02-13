using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id);
    Task<IEnumerable<Project>> GetAllAsync();
    Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status);
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task DeleteAsync(Guid id);
    Task<int> GetActiveProjectsCountAsync();
}
