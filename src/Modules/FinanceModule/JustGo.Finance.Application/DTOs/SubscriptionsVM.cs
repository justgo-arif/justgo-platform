using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs
{
    public class SubscriptionsVM : PaginatedDTO
    {
        public List<SubscriptionsDto>? Subscriptions { get; set; }
    }
}
