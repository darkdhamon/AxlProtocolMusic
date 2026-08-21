using AxlProtocolMusic.WebApp.Services;
using Microsoft.AspNetCore.DataProtection;

namespace AxlProtocolMusic.WebApp.Tests.Services;

[TestFixture]
public sealed class DeviceIdServiceTests
{
    [Test]
    public void ProtectAndResolve_RoundTripsCanonicalGuidWithoutExposingItAsCookieValue()
    {
        var service = new DeviceIdService(new EphemeralDataProtectionProvider());
        const string canonicalDeviceId = "0123456789abcdef0123456789abcdef";

        var protectedDeviceId = service.Protect(canonicalDeviceId);

        Assert.That(protectedDeviceId, Is.Not.EqualTo(canonicalDeviceId));
        Assert.That(service.TryResolve(protectedDeviceId, out var resolvedDeviceId), Is.True);
        Assert.That(resolvedDeviceId, Is.EqualTo(canonicalDeviceId));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("attacker-selected-id")]
    [TestCase("0123456789abcdef0123456789abcdef")]
    public void TryResolve_WhenCookieIsNotProtected_ReturnsFalse(string? cookieValue)
    {
        var service = new DeviceIdService(new EphemeralDataProtectionProvider());

        Assert.That(service.TryResolve(cookieValue, out var resolvedDeviceId), Is.False);
        Assert.That(resolvedDeviceId, Is.Empty);
    }
}
