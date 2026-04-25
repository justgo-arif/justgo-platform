using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetClubsMetadata
{
    public class GetClubsMetadataQuery : PaginationParams, IRequest<PagedResult<ClubDTO>>
    { 
        public string Query { get; set; }
    }
}
