using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPayments
{
    public class GetMemberPaymentsQuery : IRequest<MemberPaymentVm>
    {
        public GetMemberPaymentsQuery(
            Guid userId,
            DateTime? fromDate,
            DateTime? toDate,
            List<string>? paymentMethods,
            List<int>? statusIds,
            string? searchText,
            string? scopeKey,
            int pageSize,
            int? lastPaymentId)
        {
            UserId = userId;
            FromDate = fromDate;
            ToDate = toDate;
            PaymentMethods = paymentMethods;
            StatusIds = statusIds;
            SearchText = searchText;
            ScopeKey = scopeKey;
            PageSize = pageSize;
            LastPaymentId = lastPaymentId;
        }
        public Guid UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string>? PaymentMethods { get; set; }
        public List<int>? StatusIds { get; set; }
        public string? SearchText { get; set; }
        public string? ScopeKey { get; set; } = "all";
        public int PageSize { get; set; }
        public int? LastPaymentId { get; set; }
    }


}
