using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentMethods;

public class GetPaymentMethodsQuery : IRequest<List<LookupIntDto>>
{
}
