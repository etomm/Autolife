using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Storage.Services;

public class InMemorySettingsManager : ISettingsManager
{
    private UserSettings _userSettings = new();
    private RepositorySettings _repositorySettings = new();

    public event Action? OnSettingsChanged;

    public UserSettings GetUserSettings()
    {
        return _userSettings;
    }

    public void UpdateUserSettings(UserSettings settings)
    {
        _userSettings = settings;
        OnSettingsChanged?.Invoke();
    }

    public RepositorySettings GetRepositorySettings()
    {
        return _repositorySettings;
    }

    public void UpdateRepositorySettings(RepositorySettings settings)
    {
        _repositorySettings = settings;
        OnSettingsChanged?.Invoke();
    }
}
