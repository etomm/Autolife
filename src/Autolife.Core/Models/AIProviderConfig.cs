namespace Autolife.Core.Models;

public class AIProviderConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public AIProviderType Type { get; set; }
    public int Priority { get; set; } = 0; // Lower number = higher priority
    public bool IsEnabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; } // For Azure OpenAI or custom endpoints
    public string Model { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime? LastSuccessfulCall { get; set; }
    public DateTime? LastFailedCall { get; set; }
    public int ConsecutiveFailures { get; set; } = 0;
    public AIProviderStatus Status { get; set; } = AIProviderStatus.Unknown;
}

public enum AIProviderType
{
    OpenAI,
    AzureOpenAI,
    Anthropic,
    LocalLlama,
    GoogleGemini,
    Mock
}

public enum AIProviderStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unavailable
}
