using Adyen.Model.Checkout;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetSavedCardDetails;

public class GetSavedCardDetailsQuery : IRequest<ListStoredPaymentMethodsResponse?>
{
    public string ShopperReference { get; set; }

    public GetSavedCardDetailsQuery(string reference)
    {
        ShopperReference = reference;
    }
}
