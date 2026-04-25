namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class StoredCard
    {
        public string? Tag { get; set; }
        public string? Metadata { get; set; }
        public string? UserSyncId { get; set; }
        public int? RecurringPaymentCustomerId { get; set; }
    }

}
