using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using JustGo.Finance.Application.Interfaces;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetSavedCardDetails;

public class GetSavedCardDetailsHandler : IRequestHandler<GetSavedCardDetailsQuery, ListStoredPaymentMethodsResponse?>
{
    private readonly IAdyenClientFactory _adyenClientFactory;

    public GetSavedCardDetailsHandler(IAdyenClientFactory adyenClientFactory)
    {
        _adyenClientFactory = adyenClientFactory;
    }

    public async Task<ListStoredPaymentMethodsResponse?> Handle(GetSavedCardDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.Checkout, cancellationToken);
            var merchantCode = await _adyenClientFactory.GetMerchantCodeAsync(cancellationToken);

            if (client == null || string.IsNullOrEmpty(merchantCode)) 
                return null;

            var service = new RecurringService(client);
            var detailedPaymentMethods = await service.GetTokensForStoredPaymentDetailsAsync(request.ShopperReference, merchantCode);
            return detailedPaymentMethods;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }
}
