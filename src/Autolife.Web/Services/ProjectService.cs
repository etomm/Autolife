using Autolife.Core.Interfaces;
using Autolife.Core.Models;
using Autolife.Storage;

namespace Autolife.Web.Services;

public class ProjectService : IProjectService
{
    private readonly GitStorageService _storage;
    private readonly IKnowledgeService _knowledgeService;

    public ProjectService(GitStorageService storage, IKnowledgeService knowledgeService)
    {
        _storage = storage;
        _knowledgeService = knowledgeService;
    }

    public async Task<List<Project>> GetAllAsync()
    {
        return await _storage.LoadAllProjectsAsync();
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _storage.LoadProjectAsync(id);
    }

    public async Task<Project> CreateAsync(Project project)
    {
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveProjectAsync(project);
        return project;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveProjectAsync(project);
        return project;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Placeholder for delete operation
        await Task.CompletedTask;
    }

    public async Task<Project> CreateFromKnowledgeAsync(Guid knowledgeId, string projectTitle)
    {
        var knowledge = await _knowledgeService.GetByIdAsync(knowledgeId);
        if (knowledge == null)
            throw new InvalidOperationException("Knowledge entry not found");

        var project = new Project
        {
            Title = projectTitle,
            Description = $"Project based on: {knowledge.Title}",
            SourceKnowledgeId = knowledgeId,
            Status = ProjectStatus.Planning,
            Tags = new List<string>(knowledge.Tags)
        };

        return await CreateAsync(project);
    }
}
