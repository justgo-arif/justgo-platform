namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class Plan
    {
        public Guid Id { get; set; }
        public int PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string MerchantName { get; set; } = string.Empty;
        public string MerchantImage { get; set; } = string.Empty; 
        public string ProductName { get; set; } = string.Empty; 
        public string ProductImageURL { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string PaymentSchedule { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty; 

        public DateTime NextPaymentDate { get; set; }

        public int TotalPayments { get; set; }

        public int CompletedPayments { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CardStatus { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty; 
        public string SchemeNo { get; set; } = string.Empty;  
    }


}
