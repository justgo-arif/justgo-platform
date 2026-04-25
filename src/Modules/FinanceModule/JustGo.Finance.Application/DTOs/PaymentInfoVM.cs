using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs
{
    public class PaymentInfoVM : PaginatedDTO
    {
        public List<PaymentInfoDto>? payments { get; set; }
    }
}
