namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class PaymentDateUpdateRequest
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal? Price { get; set; }
        public bool RevertToOriginalAmount { get; set; } = false;
        public string? Reason { get; set; } = string.Empty;
        public bool SendNotification { get; set; } = false;
    }
}
