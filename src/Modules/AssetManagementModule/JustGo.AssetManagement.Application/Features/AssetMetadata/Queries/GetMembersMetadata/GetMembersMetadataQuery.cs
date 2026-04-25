using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetMembersMetadata
{
    public class GetMembersMetadataQuery : PaginationParams, IRequest<PagedResult<MemberDTO>>
    {
        public List<string> ClubIds { get; set; }
        public string Query { get; set; }
    }
}
