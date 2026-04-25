using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallment
{

    public class GetInstallmentQuery : PaginationDateRangeFilter, IRequest<InstallmentVM>
    {
        public GetInstallmentQuery(Guid merchantId, List<int> statusIds, List<string>? installmentPlanIds, string? scopeKey, string? searchText, int pageNo, int pageSize, int? totalCount, DateTime? fromdate, DateTime? todate)
        {
            MerchantId = merchantId;
            StatusIds = statusIds;
            InstallmentPlanIds = installmentPlanIds;
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
        public List<string>? InstallmentPlanIds { get; set; }
        public string? ScopeKey { get; set; }
        public int? TotalCount { get; set; }
    }

}
