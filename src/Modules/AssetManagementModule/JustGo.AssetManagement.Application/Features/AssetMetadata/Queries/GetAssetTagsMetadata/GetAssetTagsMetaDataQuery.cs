using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetAssetTagsMetadata
{
    public class GetAssetTagsMetaDataQuery : PaginationParams, IRequest<PagedResult<SelectListItemDTO<string>>>
    { 
        public string  AssetTypeId { get; set; }
        public string Query { get; set; }
    }
}
