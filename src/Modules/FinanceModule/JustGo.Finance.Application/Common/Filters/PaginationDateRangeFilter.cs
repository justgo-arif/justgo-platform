namespace JustGo.Finance.Application.Common.Filters
{
    public class PaginationDateRangeFilter : SearchableFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
