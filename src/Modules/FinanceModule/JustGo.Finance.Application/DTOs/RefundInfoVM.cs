using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs
{
    public class RefundInfoVM : PaginatedDTO
    {
        public List<RefundInfoDto>? RefundInfos { get; set; }
    }
}
