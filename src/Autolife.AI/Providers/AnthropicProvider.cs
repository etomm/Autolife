using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.AI.Services;

public class AnthropicProvider : IAIProvider
{
    private readonly AIProviderConfig _config;

    public AnthropicProvider(AIProviderConfig config)
    {
        _config = config;
    }

    public string Name => $"Anthropic Claude ({_config.Model})";

    public async Task<bool> IsHealthyAsync()
    {
        return !string.IsNullOrWhiteSpace(_config.ApiKey);
    }

    public async Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return $"[Anthropic Response] {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }

    public async Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken = default)
    {
        var prompt = $"Summarize the following text in 2-3 sentences:\n\n{content}";
        return await GenerateCompletionAsync(prompt, cancellationToken);
    }

    public async Task<List<string>> GenerateTagsAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return new List<string> { "AI-Generated", "Claude", "Auto-Tagged" };
    }

    public async Task<List<string>> SuggestCategoriesAsync(string content, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100, cancellationToken);
        return new List<string> { "General", "Uncategorized" };
    }
}
