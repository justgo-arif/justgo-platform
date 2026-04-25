using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Cart.Commands.CreateUserCart
{
    public record CreateUserCartCommand(int UserId) : IRequest<int>;

}
