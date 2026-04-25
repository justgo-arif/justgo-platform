namespace JustGo.Finance.Application.DTOs.PaymentAccount;

public class AdyenPaymentProfileDetailsDTO
{
    public string? LegalEntityId { get; set; }
    public string? BalanceAccountId { get; set; }
    public string? Type { get; set; }
    public string? DoingBusinessAs { get; set; }
    public string? Email { get; set; }
    public string? WebData { get; set; }
    public string? Country { get; set; }
    public bool ReceiveFromBalanceAccount { get; set; }
    public bool SendToBalanceAccount { get; set; }
    public bool SendToTransferInstrument { get; set; }
    public bool ReceiveFromPlatformPayments { get; set; }
    public List<string>? Problems { get; set; }
    public string? PayoutSchedule { get; set; }
    public string? SweepId { get; set; }
    public string? SweepDescription { get; set; }
    public bool IsPaymentEnabled { get; set; }
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public string? ClubJoinLink { get; set; }
    public string? EventDirectLink { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? StatementDescriptor { get; set; }
}
