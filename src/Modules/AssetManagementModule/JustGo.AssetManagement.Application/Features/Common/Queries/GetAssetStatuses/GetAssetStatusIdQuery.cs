using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetAssetStatuses
{
    public class GetAssetStatusIdQuery : IRequest<int>
    {
        public AssetStatusType Status { get; set; }
    }
}
