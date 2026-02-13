using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Core.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly IAiService _aiService;

    public ProjectService(IProjectRepository repository, IAiService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public Task<Project?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<IEnumerable<Project>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<IEnumerable<Project>> GetByStatusAsync(ProjectStatus status)
        => _repository.GetByStatusAsync(status);

    public async Task<Project> CreateAsync(Project project)
    {
        if (string.IsNullOrWhiteSpace(project.AiGeneratedPlan) && !string.IsNullOrWhiteSpace(project.Description))
        {
            project.AiGeneratedPlan = await _aiService.GenerateProjectPlanAsync(project.Description);
            project.AiSuggestedTasks = await _aiService.GenerateTasksAsync(project.Description);
        }

        project.Progress = 0;
        return await _repository.CreateAsync(project);
    }

    public Task<Project> UpdateAsync(Project project)
        => _repository.UpdateAsync(project);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);

    public async Task<Project> AddTaskAsync(Guid projectId, ProjectTask task)
    {
        var project = await _repository.GetByIdAsync(projectId);
        if (project == null)
            throw new InvalidOperationException("Project not found");

        task.Order = project.Tasks.Any() ? project.Tasks.Max(t => t.Order) + 1 : 0;
        project.Tasks.Add(task);
        
        return await _repository.UpdateAsync(project);
    }

    public async Task<Project> CompleteTaskAsync(Guid projectId, Guid taskId)
    {
        var project = await _repository.GetByIdAsync(projectId);
        if (project == null)
            throw new InvalidOperationException("Project not found");

        var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            task.IsCompleted = true;
            task.CompletedDate = DateTime.UtcNow;
        }

        project.Progress = await CalculateProgressAsync(projectId);
        return await _repository.UpdateAsync(project);
    }

    public async Task<Project> GenerateAiPlanAsync(Project project)
    {
        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            project.AiGeneratedPlan = await _aiService.GenerateProjectPlanAsync(project.Description);
            project.AiSuggestedTasks = await _aiService.GenerateTasksAsync(project.Description);
        }

        return await _repository.UpdateAsync(project);
    }

    public async Task<int> CalculateProgressAsync(Guid projectId)
    {
        var project = await _repository.GetByIdAsync(projectId);
        if (project == null || !project.Tasks.Any())
            return 0;

        var completedTasks = project.Tasks.Count(t => t.IsCompleted);
        return (int)((double)completedTasks / project.Tasks.Count * 100);
    }
}
