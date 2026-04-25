namespace JustGo.Finance.Application.DTOs
{
    public class PurchaseMember
    {
        public int ProductId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public int MemberDocId { get; set; }
        public string? ProfilePicURL { get; set; }
    }
}
