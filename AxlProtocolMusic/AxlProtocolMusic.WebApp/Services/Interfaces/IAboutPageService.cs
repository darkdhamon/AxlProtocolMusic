using AxlProtocolMusic.WebApp.Models.Content;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IAboutPageService
{
    Task<AboutPageContent> GetAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(AboutPageContent content, CancellationToken cancellationToken = default);

    Task SeedAsync(CancellationToken cancellationToken = default);
}
