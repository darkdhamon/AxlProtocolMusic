# AxlProtocolMusic

AxlProtocolMusic is the official website and lightweight content management system for Axl Protocol Music. The app is built as an ASP.NET Core Razor Components web application backed by MongoDB, with admin-managed content for releases, news, timeline events, privacy preferences, analytics, and an optional OpenAI-powered site assistant.

## What the project does

The site combines a public-facing music catalog with built-in admin editing tools:

- Home page with a featured release carousel.
- Release catalog with search, infinite scroll, upcoming-release badges, and detail pages.
- Admin editing for releases, including credits, tracks, lyrics, tags, links, publishing state, cover art uploads, autosave, and deletion.
- News page with featured stories, scheduled/unpublished article previews for admins, and in-app create/edit/delete flows.
- About page with live admin editing for artist narrative content and creative pillars.
- Timeline page that merges releases, news, and manual timeline events into a chronological view.
- Privacy pages for visitor controls, collected-data review, and deletion of browser-linked analytics.
- Analytics for page visits, time on page, outbound link clicks, repeat visitors, and coarse region/location data.
- Admin dashboard with release counts, analytics summaries, and chatbot budget controls.
- Optional site chatbot backed by the OpenAI Responses API with budget tracking and manual disable/reset controls.

## Tech stack

- .NET 10 (`net10.0`)
- ASP.NET Core Razor Components with interactive server rendering
- MongoDB for application data and ASP.NET Identity storage
- `AspNetCore.Identity.Mongo` for admin authentication
- Azure Blob Storage for uploaded images when configured
- Local disk image storage fallback when blob storage is not configured
- Markdig for Markdown rendering
- GitHub Actions workflow for Azure App Service deployment

## Repository layout

- `AxlProtocolMusic/AxlProtocolMusic.WebApp` - main web application
- `AxlProtocolMusic/AxlProtocolMusic.WebApp.Tests` - unit test project
- `AxlProtocolMusic/AxlProtocolMusic.WebApp.IntegrationTests` - integration test project for external-provider coverage such as MongoDB via `Mongo2Go`
- `.github/workflows/main_axlprotocolmusicprodweb.yml` - CI/CD workflow for Azure App Service
- `_buildcheck` - local published-output snapshots used for build checks
- `_tools` - local tooling, including a bundled `rg.exe`

## Key runtime behavior

- The app seeds bootstrap content on startup if the database is empty:
  - admin account
  - about page content
  - news articles
  - releases
  - timeline events
- Admin bootstrap seeding only runs when `AdminBootstrap` credentials are configured.
- In development, data protection keys are stored in `.localkeys`.
- Uploaded images go to Azure Blob Storage when `ImageStorage:ConnectionString` and `ImageStorage:ContainerName` are present; otherwise they are saved under `wwwroot/uploads`.
- The chatbot only works when both `Chatbot:Enabled` is `true` and `OpenAI:ApiKey` is configured.

## Requirements

- .NET SDK 10.x
- MongoDB running locally or remotely
- Optional Azure Storage account for production image uploads
- Optional OpenAI API key for the site assistant

## Local development

1. Start MongoDB.
2. Update development configuration in `AxlProtocolMusic/AxlProtocolMusic.WebApp/appsettings.Development.json` as needed.
3. From the repository root, run:

```powershell
dotnet run --project .\AxlProtocolMusic\AxlProtocolMusic.WebApp\AxlProtocolMusic.WebApp.csproj
```

The current development settings already point to:

- `MongoDb:ConnectionString = mongodb://localhost:27017`
- `MongoDb:DatabaseName = AxlProtocolMusicDev`
- bootstrap admin username `admin`
- bootstrap admin email `admin@axlprotocolmusic.local`

After first launch, sign in with the seeded admin account and change the default password immediately.

## Configuration

The app reads configuration from:

- `appsettings.json`
- `appsettings.Development.json`
- optional `appsecrets.json`
- environment variables

Important settings:

| Setting | Purpose |
| --- | --- |
| `MongoDb:ConnectionString` | MongoDB connection string for site data and identity |
| `MongoDb:DatabaseName` | MongoDB database name |
| `AdminBootstrap:UserName` | Seeded admin username |
| `AdminBootstrap:Email` | Seeded admin email |
| `AdminBootstrap:Password` | Seeded admin password |
| `ImageStorage:ConnectionString` | Azure Blob Storage connection string |
| `ImageStorage:ContainerName` | Blob container name for uploaded images |
| `ImageStorage:UploadRoot` | Local upload root for disk storage fallback |
| `ImageStorage:MaxFileSizeBytes` | Maximum upload size |
| `Chatbot:Enabled` | Enables or disables the site chatbot |
| `OpenAI:ApiKey` | API key for chatbot requests |
| `OpenAI:Model` | Responses API model name, default `gpt-5-mini` |
| `OpenAI:BaseUrl` | OpenAI API base URL |
| `MapSettings:MapTilerKey` | Map provider key for privacy/location UI |
| `MapSettings:PrivacyLocationStyleUrl` | Map style URL for privacy/location display |
| `StartupDiagnostics:ShowOnPage` | Optional startup error page fallback |
| `StartupDiagnostics:DetailedRequestErrors` | Optional request-level detailed error mode |

## Admin workflows

Once signed in as an admin, the app supports:

- editing the About page directly on the public route
- creating and managing releases from `/releases` and `/releases/{slug}`
- creating and managing news from `/news?editor=new`
- creating manual timeline events from `/timeline?editor=new`
- updating admin credentials from `/account/edit`
- reviewing analytics and resetting chatbot budget from `/admin`

Admin traffic is intentionally excluded from analytics reporting.

## Deployment

The repository includes a GitHub Actions workflow that:

- builds `AxlProtocolMusic/AxlProtocolMusic.WebApp/AxlProtocolMusic.WebApp.csproj`
- runs only `AxlProtocolMusic/AxlProtocolMusic.WebApp.Tests/AxlProtocolMusic.WebApp.Tests.csproj` in CI right now
- publishes the app without forcing a specific Windows architecture
- deploys the artifact to the Azure Web App `axlprotocolmusicprodweb`

Before deploying, make sure production app settings and secrets are configured in Azure, especially MongoDB, admin bootstrap, image storage, and OpenAI settings.

## Notes from the current codebase

- There is no dedicated test project in the repository right now.
- The root `README.md` was previously a placeholder and did not reflect the implemented feature set.
- A local app process was running during verification, so `dotnet build` succeeded with file-lock retry warnings on `AxlProtocolMusic.WebApp.exe`.
