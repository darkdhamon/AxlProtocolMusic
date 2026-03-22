namespace AxlProtocolMusic.WebApp.Configuration;

public sealed class OpenAiChatSettings
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5-mini";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}
