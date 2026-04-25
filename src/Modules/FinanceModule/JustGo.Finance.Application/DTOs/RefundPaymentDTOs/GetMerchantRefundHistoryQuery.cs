using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.DTOs.RefundPaymentDTOs
{ 
    public class GetMerchantRefundHistoryQuery : SearchableFilter
    {
        public RequestSource Source { get; set; }
        public Guid? MerchantId { get; set; } 
        public Guid PaymentId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
    }
}
