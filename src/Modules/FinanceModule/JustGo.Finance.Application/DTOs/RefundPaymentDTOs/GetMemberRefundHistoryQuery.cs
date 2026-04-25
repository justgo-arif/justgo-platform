using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.DTOs.RefundPaymentDTOs
{
    public class GetMemberRefundHistoryQuery : SearchableFilter
    {
        public RequestSource Source { get; set; }
        public Guid PaymentId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
    }
}
