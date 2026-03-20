namespace AxlProtocolMusic.WebApp.Configuration;

public sealed class DevelopmentMongoDbSettings
{
    public const string SectionName = "DevelopmentMongoDb";

    public bool Enabled { get; init; }

    public string DataDirectory { get; init; } = ".localmongo";

    public string DatabaseName { get; init; } = "AxlProtocolMusicDev";
}
