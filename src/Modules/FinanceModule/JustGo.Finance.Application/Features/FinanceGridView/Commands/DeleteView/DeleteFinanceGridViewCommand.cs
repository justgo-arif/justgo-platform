using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.DeleteView
{
    public record DeleteFinanceGridViewCommand(
        int ViewId
    ) : IRequest<bool>;
}
