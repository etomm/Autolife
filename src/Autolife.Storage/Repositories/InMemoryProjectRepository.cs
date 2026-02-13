using System.Collections.Concurrent;
using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Storage.Repositories;

public class InMemoryProjectRepository : IProjectRepository
{
    private readonly ConcurrentDictionary<Guid, Project> _projects = new();

    public Task<Project?> GetByIdAsync(Guid id)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task<IEnumerable<Project>> GetAllAsync()
    {
        return Task.FromResult(_projects.Values.Where(p => !p.IsDeleted).AsEnumerable());
    }

    public Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status)
    {
        var results = _projects.Values
            .Where(p => !p.IsDeleted && p.Status == status);
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<Project> CreateAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        _projects.TryAdd(project.Id, project);
        return Task.FromResult(project);
    }

    public Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _projects[project.Id] = project;
        return Task.FromResult(project);
    }

    public Task DeleteAsync(Guid id)
    {
        if (_projects.TryGetValue(id, out var project))
        {
            project.IsDeleted = true;
            project.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<int> GetActiveProjectsCountAsync()
    {
        var count = _projects.Values
            .Count(p => !p.IsDeleted && p.Status == ProjectStatus.Active);
        return Task.FromResult(count);
    }
}
