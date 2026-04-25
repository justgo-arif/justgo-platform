using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAllPaymentMethods
{
    public class GetAllPaymentMethodsQuery : IRequest<List<LookupStringDto>>
    {
    }
}
