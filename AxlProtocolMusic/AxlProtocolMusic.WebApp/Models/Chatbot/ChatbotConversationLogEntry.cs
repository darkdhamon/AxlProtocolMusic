using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Chatbot;

public sealed class ChatbotConversationLogEntry : IEntity
{
    public string Id { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    public string AssistantReply { get; set; } = string.Empty;

    public string PagePath { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;
}
