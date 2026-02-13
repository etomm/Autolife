using System.Text.Json;
using Autolife.Core.Models;
using LibGit2Sharp;

namespace Autolife.Storage;

public class GitStorageService
{
    private readonly string _repositoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitStorageService(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        InitializeRepository();
    }

    private void InitializeRepository()
    {
        if (!Directory.Exists(_repositoryPath))
        {
            Directory.CreateDirectory(_repositoryPath);
        }

        if (!Repository.IsValid(_repositoryPath))
        {
            Repository.Init(_repositoryPath);
            
            // Create initial structure
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "knowledge"));
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "documents"));
            Directory.CreateDirectory(Path.Combine(_repositoryPath, "projects"));
            
            using var repo = new Repository(_repositoryPath);
            Commands.Stage(repo, "*");
            
            var signature = new Signature("Autolife", "autolife@local", DateTimeOffset.Now);
            repo.Commit("Initial repository structure", signature, signature);
        }
    }

    public async Task SaveKnowledgeAsync(KnowledgeEntry entry)
    {
        var filePath = Path.Combine(_repositoryPath, "knowledge", $"{entry.Id}.json");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        
        using var repo = new Repository(_repositoryPath);
        Commands.Stage(repo, filePath);
        
        var signature = new Signature("Autolife", "autolife@local", DateTimeOffset.Now);
        repo.Commit($"Update knowledge: {entry.Title}", signature, signature);
    }

    public async Task<KnowledgeEntry?> LoadKnowledgeAsync(Guid id)
    {
        var filePath = Path.Combine(_repositoryPath, "knowledge", $"{id}.json");
        if (!File.Exists(filePath)) return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<KnowledgeEntry>(json);
    }

    public async Task<List<KnowledgeEntry>> LoadAllKnowledgeAsync()
    {
        var knowledgePath = Path.Combine(_repositoryPath, "knowledge");
        if (!Directory.Exists(knowledgePath)) return new List<KnowledgeEntry>();

        var entries = new List<KnowledgeEntry>();
        foreach (var file in Directory.GetFiles(knowledgePath, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file);
            var entry = JsonSerializer.Deserialize<KnowledgeEntry>(json);
            if (entry != null) entries.Add(entry);
        }
        return entries;
    }

    public async Task SaveProjectAsync(Project project)
    {
        var filePath = Path.Combine(_repositoryPath, "projects", $"{project.Id}.json");
        var json = JsonSerializer.Serialize(project, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        
        using var repo = new Repository(_repositoryPath);
        Commands.Stage(repo, filePath);
        
        var signature = new Signature("Autolife", "autolife@local", DateTimeOffset.Now);
        repo.Commit($"Update project: {project.Name}", signature, signature);
    }

    public async Task<Project?> LoadProjectAsync(Guid id)
    {
        var filePath = Path.Combine(_repositoryPath, "projects", $"{id}.json");
        if (!File.Exists(filePath)) return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Project>(json);
    }

    public async Task<List<Project>> LoadAllProjectsAsync()
    {
        var projectsPath = Path.Combine(_repositoryPath, "projects");
        if (!Directory.Exists(projectsPath)) return new List<Project>();

        var projects = new List<Project>();
        foreach (var file in Directory.GetFiles(projectsPath, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file);
            var project = JsonSerializer.Deserialize<Project>(json);
            if (project != null) projects.Add(project);
        }
        return projects;
    }

    public async Task SaveDocumentMetadataAsync(Document document)
    {
        var metadataPath = Path.Combine(_repositoryPath, "documents", "metadata");
        Directory.CreateDirectory(metadataPath);
        
        var filePath = Path.Combine(metadataPath, $"{document.Id}.json");
        var json = JsonSerializer.Serialize(document, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
        
        using var repo = new Repository(_repositoryPath);
        Commands.Stage(repo, filePath);
        
        var signature = new Signature("Autolife", "autolife@local", DateTimeOffset.Now);
        repo.Commit($"Update document metadata: {document.Name}", signature, signature);
    }

    public async Task<Document?> LoadDocumentMetadataAsync(Guid id)
    {
        var filePath = Path.Combine(_repositoryPath, "documents", "metadata", $"{id}.json");
        if (!File.Exists(filePath)) return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Document>(json);
    }

    public async Task<List<Document>> LoadAllDocumentMetadataAsync()
    {
        var metadataPath = Path.Combine(_repositoryPath, "documents", "metadata");
        if (!Directory.Exists(metadataPath)) return new List<Document>();

        var documents = new List<Document>();
        foreach (var file in Directory.GetFiles(metadataPath, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file);
            var document = JsonSerializer.Deserialize<Document>(json);
            if (document != null) documents.Add(document);
        }
        return documents;
    }

    public string GetDocumentFilesPath()
    {
        var filesPath = Path.Combine(_repositoryPath, "documents", "files");
        Directory.CreateDirectory(filesPath);
        return filesPath;
    }
}
