using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData
{
    public class GetLeaseStatusMetaDataQuery : IRequest<List<SelectListItemDTO<string>>>
    {
    }
}
