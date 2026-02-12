using Autolife.Core.Interfaces;
using Autolife.Core.Models;
using Autolife.Storage;

namespace Autolife.Web.Services;

public class DocumentService : IDocumentService
{
    private readonly GitStorageService _storage;

    public DocumentService(GitStorageService storage)
    {
        _storage = storage;
    }

    public async Task<List<Document>> GetAllAsync()
    {
        return await _storage.LoadAllDocumentMetadataAsync();
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _storage.LoadDocumentMetadataAsync(id);
    }

    public async Task<Document> CreateAsync(Document document, Stream fileStream)
    {
        document.UploadedAt = DateTime.UtcNow;
        
        // Save file to storage
        var filesPath = _storage.GetDocumentFilesPath();
        var fileName = $"{document.Id}{Path.GetExtension(document.Name)}";
        var filePath = Path.Combine(filesPath, fileName);
        
        using (var fileStreamOut = File.Create(filePath))
        {
            await fileStream.CopyToAsync(fileStreamOut);
        }
        
        document.FilePath = filePath;
        document.FileSize = new FileInfo(filePath).Length;
        
        await _storage.SaveDocumentMetadataAsync(document);
        return document;
    }

    public async Task<Document> UpdateAsync(Document document)
    {
        await _storage.SaveDocumentMetadataAsync(document);
        return document;
    }

    public async Task DeleteAsync(Guid id)
    {
        // Placeholder for delete operation
        await Task.CompletedTask;
    }

    public async Task<List<Document>> GetByProjectIdAsync(Guid projectId)
    {
        var all = await GetAllAsync();
        return all.Where(d => d.ProjectId == projectId).ToList();
    }

    public async Task<Stream> GetFileStreamAsync(Guid id)
    {
        var document = await GetByIdAsync(id);
        if (document == null || !File.Exists(document.FilePath))
            throw new FileNotFoundException("Document file not found");
        
        return File.OpenRead(document.FilePath);
    }
}
