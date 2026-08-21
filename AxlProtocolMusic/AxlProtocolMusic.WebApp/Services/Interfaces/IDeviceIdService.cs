namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IDeviceIdService
{
    string Protect(string canonicalDeviceId);

    bool TryResolve(string? protectedDeviceId, out string canonicalDeviceId);
}
