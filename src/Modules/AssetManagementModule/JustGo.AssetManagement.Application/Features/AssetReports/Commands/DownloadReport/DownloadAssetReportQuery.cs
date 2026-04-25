using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetReports.Commands.DownloadReport
{
    public class DownloadAssetReportQuery : IRequest<DownloadAssetReportResponse>
    {
        public string EntityId { get; set; } = string.Empty;
        public int EntityType { get; set; }
        public string ReportId { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
    }
}