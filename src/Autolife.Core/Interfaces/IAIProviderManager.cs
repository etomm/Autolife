using Autolife.Core.Models;

namespace Autolife.Core.Interfaces;

public interface IAIProviderManager
{
    Task<List<AIProviderConfig>> GetAllProvidersAsync();
    Task<AIProviderConfig> GetProviderAsync(Guid id);
    Task<AIProviderConfig> AddProviderAsync(AIProviderConfig config);
    Task<AIProviderConfig> UpdateProviderAsync(AIProviderConfig config);
    Task DeleteProviderAsync(Guid id);
    Task<List<AIProviderConfig>> GetActiveProvidersByPriorityAsync();
    Task UpdateProviderStatusAsync(Guid id, AIProviderStatus status);
    Task RecordSuccessAsync(Guid id);
    Task RecordFailureAsync(Guid id);
}
