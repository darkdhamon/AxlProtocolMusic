using AxlProtocolMusic.WebApp.Models.Analytics;
using AxlProtocolMusic.WebApp.Services.ServiceModels;

namespace AxlProtocolMusic.WebApp.Services.Interfaces;

public interface IAnalyticsService
{
    Task RecordPageVisitAsync(PageVisitMetric metric, CancellationToken cancellationToken = default);

    Task RecordExternalLinkClickAsync(ExternalLinkClickMetric metric, CancellationToken cancellationToken = default);

    Task DeleteVisitorDataAsync(string clientId, CancellationToken cancellationToken = default);

    Task DeleteVisitorLocationDataAsync(string clientId, CancellationToken cancellationToken = default);

    Task<AnalyticsDashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<VisitorCollectedDataViewModel> GetVisitorCollectedDataAsync(string clientId, CancellationToken cancellationToken = default);
}
