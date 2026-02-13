namespace Autolife.Core.Models;

public class RepositorySettings
{
    public StorageType StorageType { get; set; } = StorageType.InMemory;
    public string LocalRepositoryPath { get; set; } = string.Empty;
    public bool AutoCommit { get; set; } = true;
    public bool AutoBackup { get; set; } = false;
}

public enum StorageType
{
    InMemory,
    LocalGit,
    SQLite,
    PostgreSQL
}
