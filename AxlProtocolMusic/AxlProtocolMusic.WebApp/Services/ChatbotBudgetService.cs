using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotBudgetService : IChatbotBudgetService
{
    private const decimal DisableThresholdUsd = 10m;
    private const decimal InputCostPerMillionUsd = 2.50m;
    private const decimal OutputCostPerMillionUsd = 15.00m;

    private readonly IRepository<ChatbotBudgetState> _budgetStateRepository;
    private readonly IRepository<ChatbotUsageRecord> _usageRecordRepository;
    private readonly IChatbotActivationMonitor _chatbotActivationMonitor;

    public ChatbotBudgetService(
        IRepository<ChatbotBudgetState> budgetStateRepository,
        IRepository<ChatbotUsageRecord> usageRecordRepository,
        IChatbotActivationMonitor chatbotActivationMonitor)
    {
        _budgetStateRepository = budgetStateRepository;
        _usageRecordRepository = usageRecordRepository;
        _chatbotActivationMonitor = chatbotActivationMonitor;
    }

    public async Task<ChatbotBudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        return MapSummary(state);
    }

    public async Task<ChatbotBudgetSummary> RecordUsageAsync(
        string model,
        long inputTokens,
        long outputTokens,
        long cachedInputTokens,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        var estimatedCost = CalculateEstimatedCostUsd(inputTokens, outputTokens);

        state.TotalInputTokens += Math.Max(0, inputTokens);
        state.TotalOutputTokens += Math.Max(0, outputTokens);
        state.TotalCachedInputTokens += Math.Max(0, cachedInputTokens);
        state.TotalRequestCount += 1;
        state.TotalEstimatedCostUsd += estimatedCost;
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        var triggeredDisable = false;
        if (!state.IsDisabled && state.TotalEstimatedCostUsd >= DisableThresholdUsd)
        {
            state.IsDisabled = true;
            state.DisabledReason = $"Estimated chatbot spend reached the ${DisableThresholdUsd:0.00} limit.";
            triggeredDisable = true;
        }

        await _budgetStateRepository.UpdateAsync(state, cancellationToken);
        await _usageRecordRepository.CreateAsync(new ChatbotUsageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Model = model.Trim(),
            InputTokens = Math.Max(0, inputTokens),
            OutputTokens = Math.Max(0, outputTokens),
            CachedInputTokens = Math.Max(0, cachedInputTokens),
            EstimatedCostUsd = estimatedCost,
            WasSuccessful = true,
            TriggeredDisable = triggeredDisable
        }, cancellationToken);
        await PublishActivationStateAsync(state, cancellationToken);

        return MapSummary(state);
    }

    public async Task DisableForQuotaErrorAsync(
        string model,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        state.TotalRequestCount += 1;
        state.IsDisabled = true;
        state.DisabledReason = "Chatbot disabled after an OpenAI quota error. Reset it from the admin dashboard after quota is restored.";
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        await _budgetStateRepository.UpdateAsync(state, cancellationToken);
        await _usageRecordRepository.CreateAsync(new ChatbotUsageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Model = model.Trim(),
            WasQuotaError = true,
            ErrorCode = errorCode.Trim(),
            ErrorMessage = errorMessage.Trim(),
            WasSuccessful = false,
            TriggeredDisable = true
        }, cancellationToken);
        await PublishActivationStateAsync(state, cancellationToken);
    }

    public async Task RecordFailureAsync(
        string model,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        state.TotalRequestCount += 1;
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        await _budgetStateRepository.UpdateAsync(state, cancellationToken);
        await _usageRecordRepository.CreateAsync(new ChatbotUsageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Model = model.Trim(),
            ErrorCode = errorCode.Trim(),
            ErrorMessage = errorMessage.Trim(),
            WasSuccessful = false
        }, cancellationToken);
        await PublishActivationStateAsync(state, cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        state.TotalInputTokens = 0;
        state.TotalOutputTokens = 0;
        state.TotalCachedInputTokens = 0;
        state.TotalRequestCount = 0;
        state.TotalEstimatedCostUsd = 0;
        state.IsDisabled = false;
        state.DisabledReason = string.Empty;
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;
        state.LastResetUtc = state.LastUpdatedUtc;

        await _budgetStateRepository.UpdateAsync(state, cancellationToken);
        await PublishActivationStateAsync(state, cancellationToken);
    }

    public async Task<ChatbotBudgetSummary> SetManualDisabledAsync(
        bool isDisabled,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateStateAsync(cancellationToken);
        state.IsManuallyDisabled = isDisabled;
        state.ManualDisabledReason = isDisabled
            ? "Chatbot manually disabled from the admin dashboard."
            : string.Empty;
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        await _budgetStateRepository.UpdateAsync(state, cancellationToken);
        await PublishActivationStateAsync(state, cancellationToken);
        return MapSummary(state);
    }

    private async Task<ChatbotBudgetState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var existing = await _budgetStateRepository.GetByIdAsync(ChatbotBudgetState.SingletonId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = new ChatbotBudgetState
        {
            Id = ChatbotBudgetState.SingletonId,
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        await _budgetStateRepository.CreateAsync(created, cancellationToken);
        return created;
    }

    private static ChatbotBudgetSummary MapSummary(ChatbotBudgetState state)
    {
        var disabledReason = state.IsManuallyDisabled
            ? (string.IsNullOrWhiteSpace(state.ManualDisabledReason)
                ? "Chatbot manually disabled from the admin dashboard."
                : state.ManualDisabledReason)
            : state.DisabledReason;

        return new ChatbotBudgetSummary
        {
            TotalInputTokens = state.TotalInputTokens,
            TotalOutputTokens = state.TotalOutputTokens,
            TotalCachedInputTokens = state.TotalCachedInputTokens,
            TotalRequestCount = state.TotalRequestCount,
            TotalEstimatedCostUsd = state.TotalEstimatedCostUsd,
            DisableThresholdUsd = DisableThresholdUsd,
            IsDisabled = state.IsDisabled || state.IsManuallyDisabled,
            IsManuallyDisabled = state.IsManuallyDisabled,
            DisabledReason = disabledReason,
            LastResetUtc = state.LastResetUtc,
            LastUpdatedUtc = state.LastUpdatedUtc == default ? null : state.LastUpdatedUtc
        };
    }

    private static decimal CalculateEstimatedCostUsd(long inputTokens, long outputTokens)
    {
        var normalizedInput = Math.Max(0, inputTokens);
        var normalizedOutput = Math.Max(0, outputTokens);

        var inputCost = (normalizedInput / 1_000_000m) * InputCostPerMillionUsd;
        var outputCost = (normalizedOutput / 1_000_000m) * OutputCostPerMillionUsd;

        return decimal.Round(inputCost + outputCost, 6, MidpointRounding.AwayFromZero);
    }

    private async Task PublishActivationStateAsync(
        ChatbotBudgetState state,
        CancellationToken cancellationToken)
    {
        await _chatbotActivationMonitor.PublishAsync(new ChatbotActivationState
        {
            IsDisabled = state.IsDisabled || state.IsManuallyDisabled,
            IsManuallyDisabled = state.IsManuallyDisabled,
            DisabledReason = state.IsManuallyDisabled
                ? state.ManualDisabledReason
                : state.DisabledReason,
            LastUpdatedUtc = state.LastUpdatedUtc == default ? null : state.LastUpdatedUtc
        }, cancellationToken);
    }
}
