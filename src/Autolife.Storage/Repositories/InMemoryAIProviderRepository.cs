using System.Collections.Concurrent;
using Autolife.Core.Interfaces;
using Autolife.Core.Models;

namespace Autolife.Storage.Repositories;

public class InMemoryAIProviderRepository : IAIProviderManager
{
    private readonly ConcurrentDictionary<Guid, AIProviderConfig> _providers = new();

    public InMemoryAIProviderRepository()
    {
        // Initialize with a default mock provider
        var mockProvider = new AIProviderConfig
        {
            Name = "Mock Provider",
            Type = AIProviderType.Mock,
            Priority = 999,
            IsEnabled = true,
            Model = "mock-v1",
            Status = AIProviderStatus.Healthy
        };
        _providers.TryAdd(mockProvider.Id, mockProvider);
    }

    public Task<List<AIProviderConfig>> GetAllProvidersAsync()
    {
        return Task.FromResult(_providers.Values.ToList());
    }

    public Task<AIProviderConfig> GetProviderAsync(Guid id)
    {
        _providers.TryGetValue(id, out var provider);
        return Task.FromResult(provider!);
    }

    public Task<AIProviderConfig> AddProviderAsync(AIProviderConfig config)
    {
        _providers.TryAdd(config.Id, config);
        return Task.FromResult(config);
    }

    public Task<AIProviderConfig> UpdateProviderAsync(AIProviderConfig config)
    {
        _providers[config.Id] = config;
        return Task.FromResult(config);
    }

    public Task DeleteProviderAsync(Guid id)
    {
        _providers.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<List<AIProviderConfig>> GetActiveProvidersByPriorityAsync()
    {
        var providers = _providers.Values
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority)
            .ToList();
        return Task.FromResult(providers);
    }

    public Task UpdateProviderStatusAsync(Guid id, AIProviderStatus status)
    {
        if (_providers.TryGetValue(id, out var provider))
        {
            provider.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task RecordSuccessAsync(Guid id)
    {
        if (_providers.TryGetValue(id, out var provider))
        {
            provider.LastSuccessfulCall = DateTime.UtcNow;
            provider.ConsecutiveFailures = 0;
            provider.Status = AIProviderStatus.Healthy;
        }
        return Task.CompletedTask;
    }

    public Task RecordFailureAsync(Guid id)
    {
        if (_providers.TryGetValue(id, out var provider))
        {
            provider.LastFailedCall = DateTime.UtcNow;
            provider.ConsecutiveFailures++;
            provider.Status = provider.ConsecutiveFailures >= 5 
                ? AIProviderStatus.Unavailable 
                : AIProviderStatus.Degraded;
        }
        return Task.CompletedTask;
    }
}
