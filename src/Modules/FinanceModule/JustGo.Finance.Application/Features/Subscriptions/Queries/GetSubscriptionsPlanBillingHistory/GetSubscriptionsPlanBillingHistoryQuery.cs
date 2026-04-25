using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlanBillingHistory
{
    public class GetSubscriptionsPlanBillingHistoryQuery : SearchableFilter, IRequest<PaginatedResponse<RecurringPaymentHistory>>
    {
        public Guid MerchantId { get; set; }
        public Guid PlanId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
        public RecurringType RecurringType { get; set; }

        public GetSubscriptionsPlanBillingHistoryQuery(Guid merchantId, Guid planId, string? searchText, RecurringType recurringType, int pageNo, int pageSize, string? columnName, string? orderBy)
        {
            MerchantId = merchantId;
            PlanId = planId;
            SearchText = searchText;
            RecurringType = recurringType;
            PageNo = pageNo;
            PageSize = pageSize;
            ColumnName = columnName;
            OrderBy = orderBy;
        }
    }
}
