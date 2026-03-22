namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface ISiteChatbotContextBuilder
{
    Task<string> BuildAsync(CancellationToken cancellationToken = default);
}
