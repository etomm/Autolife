using Autolife.AI.Interfaces;

namespace Autolife.AI.Agents;

public class DocumentSummaryAgent : IAIAgent
{
    private readonly IAIProvider _aiProvider;

    public string Name => "Document Summary Agent";
    public string Description => "Generates summaries and extracts key information from documents";

    public DocumentSummaryAgent(IAIProvider aiProvider)
    {
        _aiProvider = aiProvider;
    }

    public async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        try
        {
            if (task.TaskType == "summarize")
            {
                var content = task.Parameters.GetValueOrDefault("content", string.Empty).ToString() ?? string.Empty;
                var prompt = $"Provide a concise summary of this document:\n\n{content}";
                
                var response = await _aiProvider.CompleteAsync(prompt);
                
                return new AgentResult
                {
                    Success = true,
                    Message = "Document summarized successfully",
                    Data = new Dictionary<string, object>
                    {
                        ["summary"] = response
                    }
                };
            }

            return new AgentResult
            {
                Success = false,
                Message = $"Unknown task type: {task.TaskType}",
                Errors = new List<string> { "Task type not supported" }
            };
        }
        catch (Exception ex)
        {
            return new AgentResult
            {
                Success = false,
                Message = "Agent execution failed",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
