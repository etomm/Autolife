using Autolife.AI.Interfaces;

namespace Autolife.AI.Providers;

/// <summary>
/// Mock AI provider for testing and development without requiring API keys
/// </summary>
public class MockAIProvider : IAIProvider
{
    public string Name => "Mock AI Provider";
    public bool IsConfigured => true;

    public Task<string> CompleteAsync(string prompt, AICompletionOptions? options = null)
    {
        // Simulate AI response based on prompt keywords
        if (prompt.ToLower().Contains("summar"))
        {
            return Task.FromResult("This is a mock summary of the content. In a real implementation, this would use an actual AI model to generate intelligent summaries.");
        }
        
        if (prompt.ToLower().Contains("search") || prompt.ToLower().Contains("find"))
        {
            return Task.FromResult("Mock search results: Found 3 relevant entries in your knowledge base.");
        }
        
        if (prompt.ToLower().Contains("suggest") || prompt.ToLower().Contains("recommend"))
        {
            return Task.FromResult("Mock suggestion: Based on your request, I recommend organizing your content with tags and creating project milestones.");
        }

        return Task.FromResult($"Mock AI response to: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");
    }

    public Task<List<string>> GenerateEmbeddingsAsync(string text)
    {
        // Return mock embeddings (in reality this would be a vector representation)
        var mockEmbeddings = Enumerable.Range(0, 10)
            .Select(i => (i * 0.1).ToString())
            .ToList();
        
        return Task.FromResult(mockEmbeddings);
    }
}
