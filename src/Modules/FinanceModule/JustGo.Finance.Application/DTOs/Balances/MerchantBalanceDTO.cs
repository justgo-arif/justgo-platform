using Adyen.Model.BalancePlatform;

namespace JustGo.Finance.Application.DTOs.Balances;

public class MerchantBalanceDTO
{
    public BalanceAccount.StatusEnum? Status { get; set; }
    public string? AccountHolderId { get; set; }
    public List<BalanceDto>? Balances { get; set; }
    public string? DefaultCurrencyCode { get; set; }
    public string? Description { get; set; }
    public string? Id { get; set; }
    public object? Metadata { get; set; }
    public string? MigratedAccountCode { get; set; }
    public object? PlatformPaymentConfiguration { get; set; }
    public string? Reference { get; set; }
    public string? TimeZone { get; set; }
}

public class BalanceDto
{
    public decimal Available { get; set; }
    public decimal _Balance { get; set; }
    public string? Currency { get; set; }
    public decimal Pending { get; set; }
    public decimal? Reserved { get; set; }
}