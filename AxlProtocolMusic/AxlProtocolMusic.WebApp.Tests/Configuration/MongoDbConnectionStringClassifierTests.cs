using AxlProtocolMusic.WebApp.Configuration;

namespace AxlProtocolMusic.WebApp.Tests.Configuration;

[TestFixture]
public sealed class MongoDbConnectionStringClassifierTests
{
    [TestCase("mongodb://localhost:27017", true)]
    [TestCase("mongodb://127.0.0.1:27017", true)]
    [TestCase("mongodb://[::1]:27017", true)]
    [TestCase("mongodb://localhost:27017,127.0.0.1:27018", true)]
    [TestCase("mongodb://mongo.example.com:27017", false)]
    [TestCase("mongodb://localhost:27017,mongo.example.com:27018", false)]
    [TestCase("mongodb+srv://cluster0.example.mongodb.net", false)]
    [TestCase("", false)]
    [TestCase(" ", false)]
    public void IsLocal_ReturnsExpectedResult(string connectionString, bool expected)
    {
        var result = MongoDbConnectionStringClassifier.IsLocal(connectionString);

        Assert.That(result, Is.EqualTo(expected));
    }
}
