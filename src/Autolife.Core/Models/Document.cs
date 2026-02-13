namespace Autolife.Core.Models;

public class Document : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string GitPath { get; set; } = string.Empty;
    public string GitCommitHash { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    
    // Relationships
    public Guid? ProjectId { get; set; }
    public List<Guid> RelatedKnowledgeIds { get; set; } = new();
    
    // AI Generated Fields
    public string AiExtractedText { get; set; } = string.Empty;
    public string AiGeneratedSummary { get; set; } = string.Empty;
    public List<string> AiSuggestedCategories { get; set; } = new();
}
