using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Tests.ContentModels;

[TestFixture]
public sealed class ReleaseCreditTests
{
    [Test]
    public void LegacyRole_WhenValueIsNew_AddsRole()
    {
        var credit = new ReleaseCredit();

        credit.LegacyRole = "Producer";

        Assert.That(credit.Roles, Is.EqualTo(["Producer"]));
    }

    [Test]
    public void LegacyRole_WhenValueAlreadyExists_IgnoresDuplicateCaseInsensitively()
    {
        var credit = new ReleaseCredit
        {
            Roles = ["Producer"]
        };

        credit.LegacyRole = "producer";

        Assert.That(credit.Roles, Is.EqualTo(["Producer"]));
    }

    [Test]
    public void LegacyRole_WhenValueIsNullOrWhitespace_DoesNothing()
    {
        var credit = new ReleaseCredit();

        credit.LegacyRole = null;
        credit.LegacyRole = " ";

        Assert.That(credit.Roles, Is.Empty);
        Assert.That(credit.LegacyRole, Is.Null);
    }
}
