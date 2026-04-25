namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class RecurringPaymentCardInfo
    {
        public string? UserSyncId { get; set; } 
        public int RecurringPaymentCustomerId { get; set; }
        public string? CardStatus { get; set; }
        public string? Expires { get; set; }
        public string? CardName { get; set; }
    }

}
