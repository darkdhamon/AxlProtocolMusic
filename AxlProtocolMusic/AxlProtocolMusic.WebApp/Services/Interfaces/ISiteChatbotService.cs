using AxlProtocolMusic.WebApp.Models.Chatbot;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface ISiteChatbotService
{
    Task<ChatbotMessageResponse> GenerateReplyAsync(
        string message,
        IReadOnlyList<ChatbotConversationMessage>? history = null,
        ChatbotPageContext? currentPage = null,
        CancellationToken cancellationToken = default);
}
