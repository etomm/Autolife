namespace Autolife.AI.Interfaces;

public interface IAIAgent
{
    string Name { get; }
    string Description { get; }
    Task<AgentResult> ExecuteAsync(AgentTask task);
}

public class AgentTask
{
    public string TaskType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Context { get; set; } = string.Empty;
}

public class AgentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
