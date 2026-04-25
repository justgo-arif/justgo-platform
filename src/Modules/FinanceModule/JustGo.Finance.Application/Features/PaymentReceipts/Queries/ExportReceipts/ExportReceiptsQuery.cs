using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.ExportDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.ExportReceipts
{
    public class ExportReceiptsQuery : DateRangeFilter, IRequest<ExportResultDto>
    {
        public ExportReceiptsQuery(Guid merchantId, DateTime? fromDate, DateTime? toDate,
          List<string>? paymentIds, List<string>? paymentMethods, List<int>? statusIds, string? scopeKey, string? searchText,
          string? columnName, string? orderBy)
        {
            MerchantId = merchantId;
            FromDate = fromDate;
            ToDate = toDate;
            PaymentIds = paymentIds;
            PaymentMethods = paymentMethods;
            StatusIds = statusIds;
            ScopeKey = scopeKey;
            SearchText = searchText;
            ColumnName = columnName;
            OrderBy = orderBy;
        }
        public Guid MerchantId { get; set; }
        public List<string>? PaymentIds { get; set; }
        public List<string>? PaymentMethods { get; set; }
        public List<int>? StatusIds { get; set; }
        public string? ScopeKey { get; set; }
        public string? SearchText { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
    }
}
