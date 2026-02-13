using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.AI.Services;

public class FallbackAiService : IAiService
{
    private readonly IAIProviderManager _providerManager;
    private readonly IServiceProvider _serviceProvider;

    public FallbackAiService(IAIProviderManager providerManager, IServiceProvider serviceProvider)
    {
        _providerManager = providerManager;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> GenerateSummaryAsync(string content)
    {
        return await ExecuteWithFallbackAsync(async provider => 
            await provider.GenerateSummaryAsync(content));
    }

    public async Task<List<string>> GenerateTagsAsync(string content)
    {
        return await ExecuteWithFallbackAsync(async provider => 
            await provider.GenerateTagsAsync(content));
    }

    public async Task<List<string>> SuggestCategoriesAsync(string content)
    {
        return await ExecuteWithFallbackAsync(async provider => 
            await provider.SuggestCategoriesAsync(content));
    }

    public async Task<string> ExtractTextFromDocumentAsync(byte[] documentBytes, string mimeType)
    {
        // Simple text extraction - can be enhanced with actual OCR/PDF parsing
        if (mimeType == "text/plain")
        {
            return System.Text.Encoding.UTF8.GetString(documentBytes);
        }
        return "[Text extraction requires additional libraries for this file type]";
    }

    public async Task<string> GenerateProjectPlanAsync(string projectDescription)
    {
        return await ExecuteWithFallbackAsync(async provider =>
        {
            var prompt = $"Create a detailed project plan for: {projectDescription}\n\nProvide a structured plan with phases and milestones.";
            return await provider.GenerateCompletionAsync(prompt);
        });
    }

    public async Task<List<string>> GenerateTasksAsync(string projectDescription)
    {
        var result = await ExecuteWithFallbackAsync(async provider =>
        {
            var prompt = $"Generate a list of 5-10 specific tasks for this project: {projectDescription}\n\nProvide only the task titles, one per line.";
            return await provider.GenerateCompletionAsync(prompt);
        });

        return result.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().TrimStart('-', '*', '•').Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(10)
            .ToList();
    }

    public async Task<string> AnswerQuestionAsync(string question, string context)
    {
        return await ExecuteWithFallbackAsync(async provider =>
        {
            var prompt = $"Context: {context}\n\nQuestion: {question}\n\nAnswer:";
            return await provider.GenerateCompletionAsync(prompt);
        });
    }

    private async Task<T> ExecuteWithFallbackAsync<T>(Func<IAIProvider, Task<T>> operation)
    {
        var providers = await _providerManager.GetActiveProvidersByPriorityAsync();
        
        if (!providers.Any())
        {
            throw new InvalidOperationException("No AI providers configured or enabled.");
        }

        Exception? lastException = null;

        foreach (var config in providers)
        {
            if (config.ConsecutiveFailures >= 5)
            {
                // Skip providers that have failed too many times
                continue;
            }

            try
            {
                var provider = CreateProvider(config);
                var result = await operation(provider);
                
                await _providerManager.RecordSuccessAsync(config.Id);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await _providerManager.RecordFailureAsync(config.Id);
                
                // Continue to next provider
                continue;
            }
        }

        throw new InvalidOperationException(
            $"All AI providers failed. Last error: {lastException?.Message}",
            lastException);
    }

    private IAIProvider CreateProvider(AIProviderConfig config)
    {
        return config.Type switch
        {
            AIProviderType.OpenAI => new OpenAIProvider(config),
            AIProviderType.AzureOpenAI => new AzureOpenAIProvider(config),
            AIProviderType.Anthropic => new AnthropicProvider(config),
            AIProviderType.Mock => new MockAIProvider(config),
            _ => throw new NotSupportedException($"Provider type {config.Type} not supported")
        };
    }
}
