using AxlProtocolMusic.WebApp.Services.Interfaces;
using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotActivationPollingService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IChatbotActivationMonitor _chatbotActivationMonitor;
    private readonly ILogger<ChatbotActivationPollingService> _logger;

    public ChatbotActivationPollingService(
        IServiceScopeFactory serviceScopeFactory,
        IChatbotActivationMonitor chatbotActivationMonitor,
        ILogger<ChatbotActivationPollingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _chatbotActivationMonitor = chatbotActivationMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_chatbotActivationMonitor.ActiveSubscriberCount > 0)
                {
                    await PublishLatestStateAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to poll the chatbot activation state.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task PublishLatestStateAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var chatbotBudgetService = scope.ServiceProvider.GetRequiredService<IChatbotBudgetService>();
        var summary = await chatbotBudgetService.GetSummaryAsync(cancellationToken);

        await _chatbotActivationMonitor.PublishAsync(new ChatbotActivationState
        {
            IsDisabled = summary.IsDisabled,
            IsManuallyDisabled = summary.IsManuallyDisabled,
            DisabledReason = summary.DisabledReason,
            LastUpdatedUtc = summary.LastUpdatedUtc
        }, cancellationToken);
    }
}
