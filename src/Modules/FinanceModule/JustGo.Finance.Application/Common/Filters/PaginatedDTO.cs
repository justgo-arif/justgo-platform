namespace JustGo.Finance.Application.Common.Filters
{
    public class PaginatedDTO
    {
        public int? TotalCount { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }
}
