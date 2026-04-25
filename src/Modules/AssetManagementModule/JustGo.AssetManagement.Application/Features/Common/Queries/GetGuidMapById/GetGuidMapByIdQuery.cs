using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetGuidMapById
{
    public class GetGuidMapByIdQuery : IRequest<List<MapItemDTO<decimal, string>>>
    {

        public List<decimal> Ids { get; set; }
        public AssetTables Entity { get; set; }
    }
}
