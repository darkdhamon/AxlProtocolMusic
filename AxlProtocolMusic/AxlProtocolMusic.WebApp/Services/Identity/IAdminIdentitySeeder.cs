namespace AxlProtocolMusic.WebApp.Services.Identity;

public interface IAdminIdentitySeeder
{
    Task SeedAsync();

    Task ResetBootstrapAdminAsync();
}
