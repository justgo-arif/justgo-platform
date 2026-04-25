using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetUserPaymentInfo
{

    public class GetUserPaymentInfoQuery : IRequest<List<UserPaymentInfoDto>>
    {
        public Guid OwnerId { get; set; }
        public List<Guid> UserIds { get; set; }
    }

}
