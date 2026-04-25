namespace JustGo.Finance.Application.DTOs
{
    public class RefundableItem
    {
        public int RowId { get; set; }
        public string? Code { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemDescription { get; set; } = string.Empty;
        public string? ProductImageURL { get; set; }
        public string? Comment { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal AmountRemaining { get; set; }
        public decimal AmountToRefund { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public int ForEntityDocId { get; set; }
        public string? ProfilePicURL { get; set; }
    }
}
