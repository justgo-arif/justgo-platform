using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.MemberDTOs;

namespace JustGo.Finance.Application.Features.Members.Queries.GetMemberList
{
    public class GetMemberListQuery : SearchableFilter, IRequest<PaginatedResponse<UserDocumentInfo>>
    {
        public GetMemberListQuery(Guid merchantId, List<string>? userSyncIds, string? searchText, int pageNo, int pageSize, int totalCount)
        {
            MerchantId = merchantId;
            UserSyncIds = userSyncIds;
            SearchText = searchText;
            PageNo = pageNo;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
        public Guid MerchantId { get; set; }
        public List<string>? UserSyncIds { get; set; } 
        public int TotalCount { get; set; }
    }
}
