using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.ProductDTOs
{
    public class GetPaymentProductRequestQuery : SearchableFilter
    {
        public Guid? MerchantId { get; set; }

        public Guid PaymentId { get; set; }
    }
}
