using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentUpcomingSchedule
{

    public class GetInstallmentUpcomingScheduleQuery : SearchableFilter, IRequest<PaginatedResponse<RecurringPaymentScheduleDto>>
    {
        public Guid MerchantId { get; set; }
        public Guid PlanId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }

        public GetInstallmentUpcomingScheduleQuery(Guid merchantId, Guid planId, string? searchText, int pageNo, int pageSize, string? columnName, string? orderBy)
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
