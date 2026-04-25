namespace JustGo.Finance.Application.DTOs
{
    public class PaymentReceiptsRequest
    {
        public Guid MerchantId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string>? PaymentMethods { get; set; }
        public List<int>? StatusId { get; set; }
        public string? SearchText { get; set; }
        public string? ColumnName { get; set; }
        public string? OrderBy { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public int? TotalCount { get; set; }
        public void Normalize()
        {
            if (PageNo <= 0) PageNo = 1;
            if (PageSize <= 0) PageSize = 10;

            if (string.IsNullOrWhiteSpace(SearchText) || SearchText == "string") SearchText = null;
            if (string.IsNullOrWhiteSpace(ColumnName) || ColumnName == "string") ColumnName = "DocId";
            if (string.IsNullOrWhiteSpace(OrderBy) || OrderBy == "string") OrderBy = "ASC";
            if (PaymentMethods?.Count == 1 && PaymentMethods[0] == "string") PaymentMethods.Clear();
        }
    }
}
