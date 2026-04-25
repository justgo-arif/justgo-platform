using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.ShareView
{
    public record ShareFinanceGridViewCommand(
        int ViewId,
        bool IsShared
    ) : IRequest<bool>;
}
