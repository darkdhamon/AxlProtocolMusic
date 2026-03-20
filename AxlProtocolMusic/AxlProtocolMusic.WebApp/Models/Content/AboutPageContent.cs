using AxlProtocolMusic.WebApp.Models;

namespace AxlProtocolMusic.WebApp.Models.Content;

public sealed class AboutPageContent : IEntity
{
    public const string SingletonId = "about-page";

    public string Id { get; set; } = SingletonId;

    public string HeroLead { get; set; } = string.Empty;

    public string HeroBody { get; set; } = string.Empty;

    public List<string> FocusPoints { get; set; } = [];

    public string WhyThisSiteExistsMarkdown { get; set; } = string.Empty;

    public List<string> NarrativeHighlights { get; set; } = [];

    public string OriginMarkdown { get; set; } = string.Empty;

    public List<AboutPillar> Pillars { get; set; } = [];
}
