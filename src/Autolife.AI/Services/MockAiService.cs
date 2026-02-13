using Autolife.Core.Interfaces;

namespace Autolife.AI.Services;

public class MockAiService : IAiService
{
    public Task<string> GenerateSummaryAsync(string content)
    {
        var summary = content.Length > 200 
            ? content.Substring(0, 200) + "..."
            : content;
        return Task.FromResult($"Summary: {summary}");
    }

    public Task<List<string>> GenerateTagsAsync(string content)
    {
        var tags = new List<string> { "General", "Auto-Generated" };
        return Task.FromResult(tags);
    }

    public Task<List<string>> SuggestCategoriesAsync(string content)
    {
        var categories = new List<string> { "Uncategorized", "General" };
        return Task.FromResult(categories);
    }

    public Task<string> ExtractTextFromDocumentAsync(byte[] documentBytes, string mimeType)
    {
        return Task.FromResult("[Text extracted from document]");
    }

    public Task<string> GenerateProjectPlanAsync(string projectDescription)
    {
        return Task.FromResult(
            $"Project Plan:\n" +
            $"1. Planning Phase\n" +
            $"2. Execution Phase\n" +
            $"3. Review Phase\n" +
            $"\nBased on: {projectDescription}"
        );
    }

    public Task<List<string>> GenerateTasksAsync(string projectDescription)
    {
        return Task.FromResult(new List<string>
        {
            "Define project scope",
            "Gather requirements",
            "Create implementation plan",
            "Execute tasks",
            "Review and adjust"
        });
    }

    public Task<string> AnswerQuestionAsync(string question, string context)
    {
        return Task.FromResult($"Answer to '{question}': Based on the context provided, here's a response.");
    }
}
