using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface ISettingsManager
{
    UserSettings GetUserSettings();
    void UpdateUserSettings(UserSettings settings);
    RepositorySettings GetRepositorySettings();
    void UpdateRepositorySettings(RepositorySettings settings);
    event Action? OnSettingsChanged;
}
