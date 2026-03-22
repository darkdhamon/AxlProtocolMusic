namespace AxlProtocolMusic.WebApp.Models.Chatbot;

public sealed class ChatbotMessageRequest
{
    public string Message { get; set; } = string.Empty;

    public List<ChatbotConversationMessage> History { get; set; } = [];

    public ChatbotPageContext? CurrentPage { get; set; }
}
