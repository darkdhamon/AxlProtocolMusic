using System.Diagnostics;
using AxlProtocolMusic.WebApp.Components.Pages;
using Bunit;

namespace AxlProtocolMusic.WebApp.Tests.Components.Pages;

[TestFixture]
public sealed class ErrorPageTests
{
    [Test]
    public void Error_WhenActivityHasRequestId_RendersTheRequestId()
    {
        using var activity = new Activity("test-request");
        activity.Start();

        using var context = new BunitContext();
        var cut = context.Render<Error>();

        Assert.That(cut.Markup, Does.Contain("An error occurred while processing your request."));
        Assert.That(cut.Markup, Does.Contain("Request ID:"));
        Assert.That(cut.Markup, Does.Contain(activity.Id));
    }

    [Test]
    public void Error_WhenNoRequestIdExists_HidesTheRequestIdSection()
    {
        var previous = Activity.Current;
        Activity.Current = null;

        try
        {
            using var context = new BunitContext();
            var cut = context.Render<Error>();

            Assert.That(cut.Markup, Does.Not.Contain("Request ID:"));
            Assert.That(cut.Markup, Does.Contain("Development Mode"));
        }
        finally
        {
            Activity.Current = previous;
        }
    }
}
