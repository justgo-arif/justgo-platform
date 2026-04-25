using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentMethods;

public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, List<LookupIntDto>>
{
    public Task<List<LookupIntDto>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var paymentMethods = Enum.GetValues(typeof(PaymentConsolePaymentMethods))
            .Cast<PaymentConsolePaymentMethods>()
            .Select(pm => new LookupIntDto
            {
                Id = (int)pm,
                Name = pm.ToString()
            })
            .ToList();

        return Task.FromResult(paymentMethods);
    }
}
