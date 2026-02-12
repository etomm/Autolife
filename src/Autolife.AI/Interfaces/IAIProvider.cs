namespace Autolife.AI.Interfaces;

public interface IAIProvider
{
    string Name { get; }
    Task<string> CompleteAsync(string prompt, AICompletionOptions? options = null);
    Task<List<string>> GenerateEmbeddingsAsync(string text);
    bool IsConfigured { get; }
}

public class AICompletionOptions
{
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public string? SystemPrompt { get; set; }
}
