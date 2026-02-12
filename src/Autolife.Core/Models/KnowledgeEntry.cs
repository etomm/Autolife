namespace Autolife.Core.Models;

public class KnowledgeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Guid> RelatedDocumentIds { get; set; } = new();
    public List<Guid> RelatedProjectIds { get; set; } = new();
    public string Category { get; set; } = string.Empty;
}
