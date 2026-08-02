using AxlProtocolMusic.WebApp.Models.Content;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace AxlProtocolMusic.WebApp.Tests.Serialization;

[TestFixture]
public sealed class AboutPageContentTests
{
    [Test]
    public void BsonRoundTrip_WhenDocumentContainsFutureFields_PreservesThem()
    {
        const string document = """
            {
                "_id": "about-page",
                "HeroLead": "Existing lead",
                "HeroBody": "Existing body",
                "FocusPoints": [],
                "WhyThisSiteExistsMarkdown": "Why",
                "NarrativeHighlights": [],
                "OriginMarkdown": "Origin",
                "Pillars": [],
                "SocialLinks": [],
                "FutureAdminSetting": "introduced by a newer deployment"
            }
            """;

        var result = BsonSerializer.Deserialize<AboutPageContent>(document);
        var roundTripped = result.ToBsonDocument();

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(AboutPageContent.SingletonId));
            Assert.That(result.HeroLead, Is.EqualTo("Existing lead"));
            Assert.That(result.SocialLinks, Is.Empty);
            Assert.That(result.ExtraElements["FutureAdminSetting"].AsString,
                Is.EqualTo("introduced by a newer deployment"));
            Assert.That(roundTripped["FutureAdminSetting"].AsString,
                Is.EqualTo("introduced by a newer deployment"));
            Assert.That(roundTripped.Contains(nameof(AboutPageContent.ExtraElements)), Is.False);
        });
    }
}
