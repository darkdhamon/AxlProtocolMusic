namespace AxlProtocolMusic.WebApp.Services.ServiceModels;

public sealed record ChatbotActivationState
{
    public bool IsDisabled { get; init; }

    public bool IsManuallyDisabled { get; init; }

    public string DisabledReason { get; init; } = string.Empty;

    public DateTimeOffset? LastUpdatedUtc { get; init; }
}
