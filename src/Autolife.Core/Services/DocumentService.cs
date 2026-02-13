using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Core.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IAiService _aiService;

    public DocumentService(IDocumentRepository repository, IAiService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public Task<Document?> GetByIdAsync(Guid id)
        => _repository.GetByIdAsync(id);

    public Task<IEnumerable<Document>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<IEnumerable<Document>> SearchAsync(string query)
        => _repository.SearchAsync(query);

    public async Task<Document> UploadAsync(string fileName, byte[] content, string? projectId = null)
    {
        var document = new Document
        {
            Name = Path.GetFileNameWithoutExtension(fileName),
            OriginalFileName = fileName,
            SizeInBytes = content.Length,
            MimeType = GetMimeType(fileName),
            ProjectId = projectId != null ? Guid.Parse(projectId) : null
        };

        // Extract text and generate summary using AI
        try
        {
            document.AiExtractedText = await _aiService.ExtractTextFromDocumentAsync(content, document.MimeType);
            if (!string.IsNullOrWhiteSpace(document.AiExtractedText))
            {
                document.AiGeneratedSummary = await _aiService.GenerateSummaryAsync(document.AiExtractedText);
                document.AiSuggestedCategories = await _aiService.SuggestCategoriesAsync(document.AiExtractedText);
            }
        }
        catch
        {
            // Continue without AI processing if it fails
        }

        return await _repository.CreateAsync(document);
    }

    public Task<Document> UpdateAsync(Document document)
        => _repository.UpdateAsync(document);

    public Task DeleteAsync(Guid id)
        => _repository.DeleteAsync(id);

    public Task<byte[]> DownloadAsync(Guid id)
    {
        // TODO: Implement file storage retrieval
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<Document> ProcessWithAiAsync(Document document)
    {
        if (!string.IsNullOrWhiteSpace(document.AiExtractedText))
        {
            document.AiGeneratedSummary = await _aiService.GenerateSummaryAsync(document.AiExtractedText);
            document.AiSuggestedCategories = await _aiService.SuggestCategoriesAsync(document.AiExtractedText);
        }

        return await _repository.UpdateAsync(document);
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
