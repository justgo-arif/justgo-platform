using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentReceipts
{
    public class GetPaymentReceiptsQuery : PaginationDateRangeFilter, IRequest<PaymentInfoVM>
    {
        public GetPaymentReceiptsQuery(Guid merchantId, DateTime? fromDate, DateTime? toDate,
            List<string>? paymentMethods, List<string>? customerIds, List<int>? statusIds, string? scopeKey, string? searchText,
            string? columnName, string? orderBy, int pageNo, int pageSize, int? totalCount)
        {
            MerchantId = merchantId;
            FromDate = fromDate;
            ToDate = toDate;
            PaymentMethods = paymentMethods;
            CustomerIds = customerIds;
            StatusIds = statusIds;
            SearchText = searchText;
            ScopeKey = scopeKey;
            ColumnName = columnName;
            OrderBy = orderBy;
            PageNo = pageNo;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
        public Guid MerchantId { get; set; }
        public List<string>? PaymentMethods { get; set; }
        public List<string>? CustomerIds { get; set; } 
        public List<int>? StatusIds { get; set; }
        public string? ScopeKey { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
        public int? TotalCount { get; set; }
    }

}
