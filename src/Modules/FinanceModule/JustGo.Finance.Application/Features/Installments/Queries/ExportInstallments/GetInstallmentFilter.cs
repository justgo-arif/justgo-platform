using JustGo.Finance.Application.Common.Filters;

namespace JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments
{
    public class GetInstallmentFilter : DateRangeFilter
    {
        public Guid MerchantId { get; set; }
        public string? ScopeKey { get; set; }
        public string? SearchText { get; set; }
        public List<int>? StatusIds { get; set; }
        public List<string>? PlanIds { get; set; }
    }
}
