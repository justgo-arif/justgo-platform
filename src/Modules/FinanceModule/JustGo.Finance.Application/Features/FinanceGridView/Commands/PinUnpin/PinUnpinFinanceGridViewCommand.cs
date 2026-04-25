using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.FinanceGridView.Commands.PinUnpin
{
    public record PinUnpinFinanceGridViewCommand(
        int ViewId, 
        bool IsPinned    // true → pin, false → unpin
    ) : IRequest<bool>;
}
