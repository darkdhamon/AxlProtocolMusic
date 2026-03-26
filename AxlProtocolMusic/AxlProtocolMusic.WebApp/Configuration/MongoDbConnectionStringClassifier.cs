using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Configuration;

public static class MongoDbConnectionStringClassifier
{
    public static bool ContainsAzureHost(string? connectionString)
    {
        return !string.IsNullOrWhiteSpace(connectionString)
            && connectionString.Contains("azure.com", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLocal(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var mongoUrl = new MongoUrl(connectionString);
        var servers = mongoUrl.Servers;

        if (!servers.Any())
        {
            return false;
        }

        return servers.All(server => IsLocalHost(server.Host));
    }

    private static bool IsLocalHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }
}
