using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotActivationMonitor
{
    int ActiveSubscriberCount { get; }

    IDisposable Subscribe(Func<ChatbotActivationState, ValueTask> listener);

    ValueTask PublishAsync(ChatbotActivationState state, CancellationToken cancellationToken = default);
}
