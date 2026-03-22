namespace AxlProtocolMusic.WebApp.Models.Chatbot;

public sealed class ChatbotMessageResponse
{
    public string Message { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool IsConfigured { get; set; }
}
