using JustGo.AssetManagement.Application.DTOs.AssetReportDTO;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetReports.Queries.GetAssetReports
{
    public class GetAssetReportsQuery : IRequest<List<AssetReportResponseDTO>>
    {
        public string AssetRegisterId { get; set; }
        public int EntityType { get; set; }
    }
}