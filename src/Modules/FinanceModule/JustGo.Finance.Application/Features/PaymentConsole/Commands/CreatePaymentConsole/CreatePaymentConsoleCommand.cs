using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole
{
    public class CreatePaymentConsoleCommand : IRequest<string>
    {
        public required PaymentConsoleBillingType BillingType { get; set; }
        public required PaymentConsolePaymentMethods PaymentMethod { get; set; }
        public required Guid PayTo { get; set; }
        public DateTime? ChargeDate { get; set; }
        public required List<PaymentConsoleCustomer> Customers { get; set; }
        public required List<PaymentConsoleProduct> Products { get; set; }
    }
}
