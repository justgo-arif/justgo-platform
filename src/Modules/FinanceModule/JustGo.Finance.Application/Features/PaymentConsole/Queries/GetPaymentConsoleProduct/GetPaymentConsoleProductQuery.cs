using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentConsoleProduct
{
    public record GetPaymentConsoleProductQuery(int OwnerId) : IRequest<List<OwnerWiseProductDto>>;
}
