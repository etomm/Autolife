namespace Autolife.Core.Models;

public class KnowledgeEntry : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string GitPath { get; set; } = string.Empty;
    public string GitCommitHash { get; set; } = string.Empty;
    
    // Relationships
    public List<Guid> RelatedProjectIds { get; set; } = new();
    public List<Guid> RelatedDocumentIds { get; set; } = new();
    
    // AI Generated Fields
    public string AiGeneratedSummary { get; set; } = string.Empty;
    public List<string> AiSuggestedTags { get; set; } = new();
}
