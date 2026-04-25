namespace JustGo.Finance.Application.Common.Filters
{
    public class PaginatedResponse<T>
    {
        public int TotalCount { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PaginatedResponse(IEnumerable<T> data, int pageNo, int pageSize, int totalCount)
        {
            Data = data;
            PageNo = pageNo;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }
}
