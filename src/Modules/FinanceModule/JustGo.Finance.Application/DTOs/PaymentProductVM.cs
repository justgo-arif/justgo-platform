using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs
{
    public class PaymentProductVM : PaginatedDTO
    {
        public List<Product>? products { get; set; }
    }
}
