using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class InstallmentVM : PaginatedDTO
    {
        public List<InstallmentDto>? Installments { get; set; }
    }
}
