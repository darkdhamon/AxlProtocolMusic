using AxlProtocolMusic.WebApp.Models.Chatbot;
using AxlProtocolMusic.WebApp.Repositories.Interfaces;
using AxlProtocolMusic.WebApp.Services.Interfaces;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotConversationLogService : IChatbotConversationLogService
{
    private const int MaxMessageLength = 4000;
    private const int MaxPagePathLength = 300;
    private const int MaxPageTitleLength = 300;
    private const int MaxOutcomeLength = 80;

    private readonly IRepository<ChatbotConversationLogEntry> _conversationLogRepository;

    public ChatbotConversationLogService(IRepository<ChatbotConversationLogEntry> conversationLogRepository)
    {
        _conversationLogRepository = conversationLogRepository;
    }

    public Task RecordAsync(
        string userMessage,
        string assistantReply,
        string outcome,
        ChatbotPageContext? currentPage = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new ChatbotConversationLogEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Outcome = Truncate(outcome, MaxOutcomeLength),
            UserMessage = Truncate(userMessage, MaxMessageLength),
            AssistantReply = Truncate(assistantReply, MaxMessageLength),
            PagePath = Truncate(currentPage?.PagePath, MaxPagePathLength),
            PageTitle = Truncate(currentPage?.PageTitle, MaxPageTitleLength)
        };

        return _conversationLogRepository.CreateAsync(entry, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatbotConversationLogEntry>> GetRecentAsync(
        int count = 25,
        CancellationToken cancellationToken = default)
    {
        var normalizedCount = Math.Clamp(count, 1, 200);
        var entries = await _conversationLogRepository.GetAllAsync(cancellationToken);

        return entries
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(normalizedCount)
            .ToList();
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength].TrimEnd()}...";
    }
}
