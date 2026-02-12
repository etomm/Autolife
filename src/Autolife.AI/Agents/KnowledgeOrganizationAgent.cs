using Autolife.AI.Interfaces;

namespace Autolife.AI.Agents;

public class KnowledgeOrganizationAgent : IAIAgent
{
    private readonly IAIProvider _aiProvider;

    public string Name => "Knowledge Organization Agent";
    public string Description => "Automatically organizes and categorizes knowledge entries";

    public KnowledgeOrganizationAgent(IAIProvider aiProvider)
    {
        _aiProvider = aiProvider;
    }

    public async Task<AgentResult> ExecuteAsync(AgentTask task)
    {
        try
        {
            if (task.TaskType == "categorize")
            {
                var content = task.Parameters.GetValueOrDefault("content", string.Empty).ToString() ?? string.Empty;
                var prompt = $"Analyze this content and suggest 3-5 relevant categories and tags:\n\n{content}";
                
                var response = await _aiProvider.CompleteAsync(prompt);
                
                return new AgentResult
                {
                    Success = true,
                    Message = "Content analyzed successfully",
                    Data = new Dictionary<string, object>
                    {
                        ["suggestions"] = response
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
