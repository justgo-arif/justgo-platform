namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class RecurringPaymentScheduleDto
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DueDate { get; set; }
        public string SchemeNo { get; set; } = string.Empty;
    }
}
