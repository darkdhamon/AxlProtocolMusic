using AxlProtocolMusic.WebApp.Models.Chatbot;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IChatbotConversationLogService
{
    Task RecordAsync(
        string userMessage,
        string assistantReply,
        string outcome,
        ChatbotPageContext? currentPage = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(
        int count = 25,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatbotConversationLogEntry>> GetExportAsync(
        int count = 5000,
        CancellationToken cancellationToken = default);
}
