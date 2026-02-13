using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.AI.Services;

public class MockAIProvider : IAIProvider
{
    private readonly AIProviderConfig _config;

    public MockAIProvider(AIProviderConfig config)
    {
        _config = config;
    }

    public string Name => "Mock AI Provider";

    public async Task<bool> IsHealthyAsync()
    {
        return true;
    }

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return $"Mock response for: {prompt.Substring(0, Math.Min(100, prompt.Length))}...";
    }

    public async Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        var summary = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
        return $"Summary: {summary}";
    }

    public async Task<List<string>> GenerateTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return new List<string> { "Mock", "Auto-Generated", "Test" };
    }

    public async Task<List<string>> SuggestCategoriesAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        return new List<string> { "General", "Mock Category" };
    }
}
