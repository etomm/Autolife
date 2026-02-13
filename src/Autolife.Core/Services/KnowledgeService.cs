using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Core.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeRepository _repository;
    private readonly IAiService _aiService;

    public KnowledgeService(IKnowledgeRepository repository, IAiService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public Task<KnowledgeEntry?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<IEnumerable<KnowledgeEntry>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<IEnumerable<KnowledgeEntry>> SearchAsync(string query)
        => _repository.SearchAsync(query);

    public async Task<KnowledgeEntry> CreateAsync(KnowledgeEntry entry)
    {
        // Auto-generate summary and tags if not provided
        if (string.IsNullOrWhiteSpace(entry.Summary) && !string.IsNullOrWhiteSpace(entry.Content))
        {
            entry.AiGeneratedSummary = await _aiService.GenerateSummaryAsync(entry.Content);
        }

        if (!entry.Tags.Any() && !string.IsNullOrWhiteSpace(entry.Content))
        {
            entry.AiSuggestedTags = await _aiService.GenerateTagsAsync(entry.Content);
        }

        return await _repository.CreateAsync(entry);
    }

    public Task<KnowledgeEntry> UpdateAsync(KnowledgeEntry entry)
        => _repository.UpdateAsync(entry);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);

    public async Task<KnowledgeEntry> EnrichWithAiAsync(KnowledgeEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Content))
        {
            entry.AiGeneratedSummary = await _aiService.GenerateSummaryAsync(entry.Content);
            entry.AiSuggestedTags = await _aiService.GenerateTagsAsync(entry.Content);
        }

        return await _repository.UpdateAsync(entry);
    }

    public async Task<Project> CreateProjectFromKnowledgeAsync(Guid knowledgeId)
    {
        var knowledge = await _repository.GetByIdAsync(knowledgeId);
        if (knowledge == null)
            throw new InvalidOperationException("Knowledge entry not found");

        var project = new Project
        {
            Name = $"Project: {knowledge.Title}",
            Description = knowledge.Summary,
            SourceKnowledgeId = knowledge.Id,
            Status = ProjectStatus.Planning,
            StartDate = DateTime.UtcNow
        };

        // Generate AI plan and tasks
        project.AiGeneratedPlan = await _aiService.GenerateProjectPlanAsync(knowledge.Content);
        var suggestedTasks = await _aiService.GenerateTasksAsync(knowledge.Content);
        
        project.Tasks = suggestedTasks.Select((task, index) => new ProjectTask
        {
            Title = task,
            Order = index
        }).ToList();

        return project;
    }
}
