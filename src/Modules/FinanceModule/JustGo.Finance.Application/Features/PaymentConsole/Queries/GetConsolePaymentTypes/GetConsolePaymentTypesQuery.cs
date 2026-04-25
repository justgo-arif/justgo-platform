using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentTypes;

public class GetConsolePaymentTypesQuery : IRequest<List<ConsolePaymentTypesDto>>
{
}
