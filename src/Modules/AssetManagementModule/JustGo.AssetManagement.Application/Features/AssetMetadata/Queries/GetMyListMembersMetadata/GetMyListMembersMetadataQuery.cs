using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetMembersMetadata
{
    public class GetMyListMembersMetadataQuery : PaginationParams, IRequest<PagedResult<MyListMemberDTO>>
    {
        public string Query { get; set; }
    }
}
