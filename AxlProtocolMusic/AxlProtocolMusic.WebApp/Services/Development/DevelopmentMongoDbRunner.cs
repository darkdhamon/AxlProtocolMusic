using Mongo2Go;

namespace AxlProtocolMusic.WebApp.Services.Development;

public sealed class DevelopmentMongoDbRunner : IDisposable
{
    public DevelopmentMongoDbRunner(MongoDbRunner runner)
    {
        Runner = runner;
    }

    public MongoDbRunner Runner { get; }

    public void Dispose()
    {
        // Mongo2Go debugging mode is intentionally persistent across app restarts.
        // We keep the runner alive for the lifetime of the app and do not dispose it here.
    }
}
