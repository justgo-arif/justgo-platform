using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundHistory
{

    public class GetRefundHistoryQuery : SearchableFilter, IRequest<RefundInfoVM>
    {
        public GetRefundHistoryQuery(RequestSource source, Guid? merchantId, Guid? memberId,Guid paymentId, string? searchText, string columnName, string orderBy, int pageNo, int pageSize)
        {
            Source = source;
            MerchantId = merchantId;
            MemberId = memberId;
            PaymentId = paymentId;
            SearchText = searchText;
            ColumnName = columnName;
            OrderBy = orderBy;
            PageNo = pageNo;
            PageSize = pageSize;
        }

        public RequestSource Source { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? MemberId { get; set; }
        public Guid PaymentId { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }

    }
}
