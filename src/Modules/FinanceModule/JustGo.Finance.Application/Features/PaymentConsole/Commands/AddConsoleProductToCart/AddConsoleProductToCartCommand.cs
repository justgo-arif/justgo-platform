using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Commands.AddProductToCart;

public class AddConsoleProductToCartCommand : IRequest<bool>
{
    public PaymentConsoleBillingType BillingType { get; set; }
    public int ProductOwnerId { get; set; }
    public List<PaymentConsoleCustomer> Customers { get; set; }
    public List<PaymentConsoleProduct> Products { get; set; }

    public AddConsoleProductToCartCommand(
        PaymentConsoleBillingType billingType,
        int productOwnerId,
        List<PaymentConsoleCustomer> customers,
        List<PaymentConsoleProduct> products)
    {
        BillingType = billingType;
        ProductOwnerId = productOwnerId;
        Customers = customers;
        Products = products;
    }
}
