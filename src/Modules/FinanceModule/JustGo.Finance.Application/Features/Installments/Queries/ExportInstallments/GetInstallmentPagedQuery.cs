using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments
{
    public class GetInstallmentPagedQuery : PaginationDateRangeFilter, IRequest<List<InstallmentDto>>
    {
        public GetInstallmentPagedQuery(Guid merchantId, RecurringType recurringType, List<int>? statusIds, List<string>? installmentPlanIds, string? scopeKey, string? searchText, DateTime? fromdate, DateTime? todate, int pageNo, int pageSize)
        {
            MerchantId = merchantId;
            RecurringType = recurringType;
            StatusIds = statusIds;
            InstallmentPlanIds = installmentPlanIds;
            ScopeKey = scopeKey;
            SearchText = searchText;
            FromDate = fromdate;
            ToDate = todate;
            PageNo = pageNo;
            PageSize = pageSize;
        }
        public Guid MerchantId { get; set; }
        public List<int>? StatusIds { get; set; }
        public List<string>? InstallmentPlanIds { get; set; }
        public RecurringType RecurringType { get; set; }
        public string? ScopeKey { get; set; }
    }
}
