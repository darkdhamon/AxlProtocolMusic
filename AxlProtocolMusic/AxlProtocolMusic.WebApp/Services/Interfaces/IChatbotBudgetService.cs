using AxlProtocolMusic.WebApp.Models.Chatbot;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotBudgetService
{
    Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<ChatbotBudgetSummary> RecordUsageAsync(
        string model,
        long inputTokens,
        long outputTokens,
        long cachedInputTokens,
        CancellationToken cancellationToken = default);

    Task DisableForQuotaErrorAsync(
        string model,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        string model,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);

    Task<ChatbotBudgetSummary> SetManualDisabledAsync(
        bool isDisabled,
        CancellationToken cancellationToken = default);
}
