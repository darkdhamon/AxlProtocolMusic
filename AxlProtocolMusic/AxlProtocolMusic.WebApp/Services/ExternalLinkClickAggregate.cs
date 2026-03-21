namespace AxlProtocolMusic.WebApp.Services;

public sealed class ExternalLinkClickAggregate
{
    public string DestinationUrl { get; init; } = string.Empty;

    public string LinkLabel { get; init; } = string.Empty;

    public int ClickCount { get; init; }

    public string SourcePagePath { get; init; } = string.Empty;
}
