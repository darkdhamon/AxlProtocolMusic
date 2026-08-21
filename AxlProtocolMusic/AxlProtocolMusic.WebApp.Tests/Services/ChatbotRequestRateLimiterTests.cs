using AxlProtocolMusic.WebApp.Services;
using MongoDB.Bson;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class ChatbotRequestRateLimiterTests
{
    [Test]
    public void BuildAtomicRateLimitOperation_UsesConditionalRollingWindowAndAtomicAppend()
    {
        var now = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);

        var (filter, updateStage) = ChatbotRequestRateLimiter.BuildAtomicRateLimitOperation(
            "DEVICE-HASH",
            now.AddMinutes(-1),
            now);

        Assert.That(filter["_id"].AsString, Is.EqualTo("DEVICE-HASH"));
        Assert.That(filter.Contains("$expr"), Is.True);
        Assert.That(filter.ToJson(), Does.Contain("$size").And.Contain("$filter").And.Contain("$lt"));
        Assert.That(updateStage.ToJson(), Does.Contain("$concatArrays").And.Contain("requests").And.Contain("expiresAt"));
    }
}
