using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentTypes;

public class GetConsolePaymentTypesHandler : IRequestHandler<GetConsolePaymentTypesQuery, List<ConsolePaymentTypesDto>>
{
    public Task<List<ConsolePaymentTypesDto>> Handle(GetConsolePaymentTypesQuery request, CancellationToken cancellationToken)
    {
        var types = Enum.GetValues(typeof(PaymentConsoleBillingType))
            .Cast<PaymentConsoleBillingType>()
            .Select(pm => new ConsolePaymentTypesDto
            {
                Id = (int)pm,
                Name = pm.ToString()
            })
            .ToList();

        return Task.FromResult(types);
    }
}
