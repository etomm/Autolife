namespace Autolife.Core.Interfaces;

public interface IAiService
{
    Task<string> GenerateSummaryAsync(string content);
    Task<List<string>> GenerateTagsAsync(string content);
    Task<List<string>> SuggestCategoriesAsync(string content);
    Task<string> ExtractTextFromDocumentAsync(byte[] documentBytes, string mimeType);
    Task<string> GenerateProjectPlanAsync(string projectDescription);
    Task<List<string>> GenerateTasksAsync(string projectDescription);
    Task<string> AnswerQuestionAsync(string question, string context);
}
