namespace Autolife.Core.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid? ProjectId { get; set; }
    public List<Guid> RelatedKnowledgeIds { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}
