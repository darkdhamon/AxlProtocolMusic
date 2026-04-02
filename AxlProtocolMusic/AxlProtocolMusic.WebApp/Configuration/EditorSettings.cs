namespace AxlProtocolMusic.WebApp.Configuration;

public sealed class EditorSettings
{
    public const string SectionName = "Editor";
    public const int DefaultAutosaveDelayMilliseconds = 2000;

    public int AutosaveDelayMilliseconds { get; set; } = DefaultAutosaveDelayMilliseconds;

    public int NormalizedAutosaveDelayMilliseconds
        => AutosaveDelayMilliseconds > 0
            ? AutosaveDelayMilliseconds
            : DefaultAutosaveDelayMilliseconds;
}
