using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentStatus
{
    public record GetPaymentStatusQuery : IRequest<List<LookupIntDto>>;
}
