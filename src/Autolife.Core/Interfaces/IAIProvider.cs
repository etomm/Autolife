namespace Autolife.Core.Interfaces;

public interface IAIProvider
{
    string Name { get; }
    Task<bool> IsHealthyAsync();
    Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> GenerateSummaryAsync(string content, CancellationToken cancellationToken = default);
    Task<List<string>> GenerateTagsAsync(string content, CancellationToken cancellationToken = default);
    Task<List<string>> SuggestCategoriesAsync(string content, CancellationToken cancellationToken = default);
}
