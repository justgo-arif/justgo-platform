using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.ProductDTOs;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentProduct
{

    public class GetPaymentProductQuery : SearchableFilter, IRequest<PaymentProductVM>
    {
        public GetPaymentProductQuery(Guid? merchantId,Guid? memberId ,Guid paymentId, string? searchText, int pageNo, int pageSize, ProductRequestSource source)
        {
            MerchantId = merchantId;
            MemberId = memberId;
            PaymentId = paymentId;
            SearchText = searchText;
            PageNo = pageNo;
            PageSize = pageSize;
            Source = source;
        }

        public Guid? MerchantId { get; set; }
        public Guid? MemberId { get; set; } 
        public Guid PaymentId { get; set; }
        public ProductRequestSource Source { get; set; }
    }
}
