using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Chatbot;

public sealed class ChatbotBudgetState : IEntity
{
    public const string SingletonId = "chatbot-budget-state";

    public string Id { get; set; } = SingletonId;

    public long TotalInputTokens { get; set; }

    public long TotalOutputTokens { get; set; }

    public long TotalCachedInputTokens { get; set; }

    public long TotalRequestCount { get; set; }

    public decimal TotalEstimatedCostUsd { get; set; }

    public bool IsDisabled { get; set; }

    public string DisabledReason { get; set; } = string.Empty;

    public bool IsManuallyDisabled { get; set; }

    public string ManualDisabledReason { get; set; } = string.Empty;

    public DateTimeOffset LastUpdatedUtc { get; set; }

    public DateTimeOffset? LastResetUtc { get; set; }
}
