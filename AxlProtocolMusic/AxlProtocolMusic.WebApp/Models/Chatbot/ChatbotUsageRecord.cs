using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Chatbot;

public sealed class ChatbotUsageRecord : IEntity
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string Model { get; set; } = string.Empty;

    public long InputTokens { get; set; }

    public long OutputTokens { get; set; }

    public long CachedInputTokens { get; set; }

    public decimal EstimatedCostUsd { get; set; }

    public bool WasSuccessful { get; set; }

    public bool TriggeredDisable { get; set; }

    public bool WasQuotaError { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}
