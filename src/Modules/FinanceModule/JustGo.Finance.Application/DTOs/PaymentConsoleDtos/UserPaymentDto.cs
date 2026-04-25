namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class UserPaymentInfoDto
    {
        public int DocId { get; set; }
        public required string MID { get; set; }
        public int MemberDocId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string? UserSyncId { get; set; } 
        public string Image { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;

        public InvoiceDetails? BillingDetails { get; set; }
        public List<RecurringPaymentCardInfo>? UserCards { get; set; }
    }

}
