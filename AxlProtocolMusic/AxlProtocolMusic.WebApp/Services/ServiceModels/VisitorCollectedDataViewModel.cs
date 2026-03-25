namespace AxlProtocolMusic.WebApp.Services;

public sealed class VisitorCollectedDataViewModel
{
    public string VisitorId { get; set; } = string.Empty;

    public bool HasVisitorId => !string.IsNullOrWhiteSpace(VisitorId);

    public bool MetricsDisabled { get; set; }

    public bool IsAdminExcluded { get; set; }

    public IReadOnlyList<VisitorPageVisitViewModel> PageVisits { get; set; } = [];

    public IReadOnlyList<VisitorExternalLinkClickViewModel> ExternalLinkClicks { get; set; } = [];
}
