namespace Autolife.Core.Models;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int Progress { get; set; } = 0;
    public string GitPath { get; set; } = string.Empty;
    public string GitCommitHash { get; set; } = string.Empty;
    
    // Relationships
    public Guid? SourceKnowledgeId { get; set; }
    public List<ProjectTask> Tasks { get; set; } = new();
    public List<Guid> DocumentIds { get; set; } = new();
    
    // AI Generated Fields
    public string AiGeneratedPlan { get; set; } = string.Empty;
    public List<string> AiSuggestedTasks { get; set; } = new();
}

public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedDate { get; set; }
    public int Order { get; set; }
}

public enum ProjectStatus
{
    Planning,
    Active,
    OnHold,
    Completed,
    Cancelled
}
