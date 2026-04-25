using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class PlanHistoryRequest : SearchableFilter
    {
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
    }
}
