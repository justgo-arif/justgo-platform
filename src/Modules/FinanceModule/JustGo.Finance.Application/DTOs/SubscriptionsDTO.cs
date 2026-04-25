namespace JustGo.Finance.Application.DTOs
{
    public class SubscriptionsDto
    {
        public Guid Id { get; set; } 
        public int PlanId { get; set; }
        public int OwnerUserId { get; set; }
        public string? ProductId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? ProfilePicURL { get; set; }
        public string? PlanOwnerMemberID { get; set; }
        public string? PlanOwnerName { get; set; }
        public string? PlanOwnerProfilePicURL { get; set; } 
        public string? PlanName { get; set; }
        public string? CreatedOn { get; set; }
        public string? NextPaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? CardStatus { get; set; }
    }
}
