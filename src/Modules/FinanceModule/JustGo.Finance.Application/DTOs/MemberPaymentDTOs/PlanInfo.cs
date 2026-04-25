namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class PlanInfo
    {
        public Guid MerchantId { get; set; }
        public Guid PlanGuid { get; set; }
        public int RecurringType { get; set; }
    }
}
