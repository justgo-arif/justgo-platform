using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.ProductDTOs
{
    public class GetMemberProductsRequestQuery : SearchableFilter
    {
        public Guid PaymentId { get; set; }
    }
}
