using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AdditionalFieldsDTO;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetStatusMetaData
{
    public class GetAssetDetailsMetaDataQuery : IRequest<List<FormModel>>
    {
        public string AssetTypeId { get; set; }
    }
}
