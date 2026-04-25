using JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole;

namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

public class PaymentConsoleCustomer
{
    public required string EntityId { get; set; }
    public int? RecurringPaymentCustomerId { get; set; }
    public BillingDetails? BillingDetails { get; set; }
}

public class BillingDetails
{
    public string? InvoiceTo { get; set; }
    public string? Email { get; set; }
    public string? ContactNo { get; set; }
    public string? Country { get; set; }
    public string? AddressLine1 { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? PostCode { get; set; }
    public string? PoNumber { get; set; }
    public string? TaxId { get; set; }
    public string? Note { get; set; }
}