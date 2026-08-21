using System.Security.Cryptography;
using System.Text;
using AxlProtocolMusic.WebApp.Configuration;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class ChatbotRequestRateLimiter : IChatbotRequestRateLimiter
{
    private const int PermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PermitLifetime = TimeSpan.FromSeconds(10);
    private readonly IMongoCollection<BsonDocument> rateLimits;
    private readonly IMongoCollection<BsonDocument> permits;
    private readonly Task indexInitialization;

    public ChatbotRequestRateLimiter(IOptions<MongoDbSettings> settings)
    {
        var value = settings.Value;
        if (string.IsNullOrWhiteSpace(value.ConnectionString) || string.IsNullOrWhiteSpace(value.DatabaseName))
        {
            throw new InvalidOperationException("MongoDb settings must be configured for chatbot rate limiting.");
        }

        var database = new MongoClient(value.ConnectionString).GetDatabase(value.DatabaseName);
        rateLimits = database.GetCollection<BsonDocument>("ChatbotRateLimitState");
        permits = database.GetCollection<BsonDocument>("ChatbotPermit");
        indexInitialization = EnsureIndexesAsync();
    }

    public async Task<string?> TryIssuePermitAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        await indexInitialization.WaitAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var cutoff = now.Subtract(Window);
        var partitionId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deviceId)));
        var (filter, updateStage) = BuildAtomicRateLimitOperation(partitionId, cutoff, now);
        var update = new PipelineUpdateDefinition<BsonDocument>(new[] { updateStage });

        var admitted = await rateLimits.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        if (admitted is null)
        {
            var partitionExists = await rateLimits
                .Find(Builders<BsonDocument>.Filter.Eq("_id", partitionId))
                .Limit(1)
                .AnyAsync(cancellationToken);
            if (partitionExists)
            {
                return null;
            }

            try
            {
                await rateLimits.InsertOneAsync(new BsonDocument
                {
                    { "_id", partitionId },
                    { "requests", new BsonArray { now } },
                    { "expiresAt", now.Add(Window).Add(Window) }
                }, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                admitted = await rateLimits.FindOneAndUpdateAsync(
                    filter,
                    update,
                    new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                    cancellationToken);
                if (admitted is null)
                {
                    return null;
                }
            }
        }

        var permitToken = Guid.NewGuid().ToString("N");
        await permits.InsertOneAsync(new BsonDocument
        {
            { "_id", permitToken },
            { "expiresAt", now.Add(PermitLifetime) }
        }, cancellationToken: cancellationToken);
        return permitToken;
    }

    internal static (BsonDocument Filter, BsonDocument UpdateStage) BuildAtomicRateLimitOperation(
        string partitionId,
        DateTime cutoff,
        DateTime now)
    {
        var activeRequests = new BsonDocument("$filter", new BsonDocument
        {
            { "input", new BsonDocument("$ifNull", new BsonArray { "$requests", new BsonArray() }) },
            { "as", "request" },
            { "cond", new BsonDocument("$gte", new BsonArray { "$$request", cutoff }) }
        });
        var filter = new BsonDocument
        {
            { "_id", partitionId },
            { "$expr", new BsonDocument("$lt", new BsonArray { new BsonDocument("$size", activeRequests), PermitLimit }) }
        };
        var updateStage = new BsonDocument("$set", new BsonDocument
            {
                { "requests", new BsonDocument("$concatArrays", new BsonArray { activeRequests, new BsonArray { now } }) },
                { "expiresAt", now.Add(Window).Add(Window) }
            });
        return (filter, updateStage);
    }

    public async Task<bool> TryConsumePermitAsync(string permitToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permitToken))
        {
            return false;
        }

        await indexInitialization.WaitAsync(cancellationToken);
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", permitToken),
            Builders<BsonDocument>.Filter.Gte("expiresAt", DateTime.UtcNow));
        return await permits.FindOneAndDeleteAsync(filter, cancellationToken: cancellationToken) is not null;
    }

    private async Task EnsureIndexesAsync()
    {
        var ttl = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("expiresAt"),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero });
        await Task.WhenAll(rateLimits.Indexes.CreateOneAsync(ttl), permits.Indexes.CreateOneAsync(ttl));
    }
}
