using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptions
{
    public class GetSubscriptionsQuery : PaginationDateRangeFilter, IRequest<SubscriptionsVM>
    {
        public GetSubscriptionsQuery(Guid merchantId, List<int> statusIds, List<string> subscriptionPlanIds, string? scopeKey, string? searchText, int pageNo, int pageSize, int? totalCount, DateTime? fromdate, DateTime? todate)
        {
            MerchantId = merchantId;
            StatusIds = statusIds;
            SubscriptionPlanIds = subscriptionPlanIds;
            ScopeKey = scopeKey;
            SearchText = searchText;
            PageNo = pageNo;
            PageSize = pageSize;
            TotalCount = totalCount;
            FromDate = fromdate;
            ToDate = todate;
        }
        public Guid MerchantId { get; set; }
        public List<int>? StatusIds { get; set; }
        public List<string>? SubscriptionPlanIds { get; set; }
        public string? ScopeKey { get; set; }
        public int? TotalCount { get; set; }
    }
}
