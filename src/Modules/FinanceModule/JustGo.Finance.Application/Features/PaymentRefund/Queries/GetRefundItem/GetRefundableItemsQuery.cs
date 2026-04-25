using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.PaymentRefundDTOs;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundProduct
{

    public class GetRefundableItemsQuery : IRequest<List<RefundableItemDto>>
    {
        public Guid? MerchantId { get; set; }

        public Guid? MemberId { get; set; }

        public Guid PaymentId { get; set; }

        public RequestSource Source { get; set; }

        public GetRefundableItemsQuery(Guid paymentId)
        {
            PaymentId = paymentId;
        }
    }

}
