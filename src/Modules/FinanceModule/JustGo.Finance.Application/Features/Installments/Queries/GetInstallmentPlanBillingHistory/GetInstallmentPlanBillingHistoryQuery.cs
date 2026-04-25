using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlanBillingHistory
{
    public class GetInstallmentPlanBillingHistoryQuery : SearchableFilter, IRequest<PaginatedResponse<RecurringPaymentHistory>>
    {
        public Guid MerchantId { get; set; }
        public Guid PlanId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }

        public GetInstallmentPlanBillingHistoryQuery(Guid merchantId, Guid planId, string? searchText, int pageNo, int pageSize, string? columnName, string? orderBy)
        {
            MerchantId = merchantId;
            PlanId = planId;
            SearchText = searchText;
            PageNo = pageNo;
            PageSize = pageSize;
            ColumnName = columnName;
            OrderBy = orderBy;
        }
    }
}
