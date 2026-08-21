using System.Security.Cryptography;
using AxlProtocolMusic.WebApp.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AxlProtocolMusic.WebApp.Services;

public sealed class DeviceIdService : IDeviceIdService
{
    private const string ProtectionPurpose = "AxlProtocolMusic.DeviceId.v1";
    private readonly IDataProtector protector;

    public DeviceIdService(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider.CreateProtector(ProtectionPurpose);
    }

    public string Protect(string canonicalDeviceId) => protector.Protect(canonicalDeviceId);

    public bool TryResolve(string? protectedDeviceId, out string canonicalDeviceId)
    {
        canonicalDeviceId = string.Empty;
        if (string.IsNullOrWhiteSpace(protectedDeviceId))
        {
            return false;
        }

        try
        {
            var candidate = protector.Unprotect(protectedDeviceId);
            if (!Guid.TryParseExact(candidate, "N", out _))
            {
                return false;
            }

            canonicalDeviceId = candidate;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
