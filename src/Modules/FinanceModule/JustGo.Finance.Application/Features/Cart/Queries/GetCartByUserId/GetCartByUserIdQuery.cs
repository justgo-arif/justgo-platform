using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Cart.Queries.GetCartByUserId;

public class GetCartByUserIdQuery : IRequest<int?>
{
    public int UserId { get; set; }
    public Guid UserGuid { get; set; }

    public GetCartByUserIdQuery(int userId, Guid userGuid)
    {
        if (userId <= 0 && userGuid == Guid.Empty)
        {
            throw new ArgumentException("You must provide either a valid UserId or a valid UserGuid.");
        }

        UserId = userId;
        UserGuid = userGuid;
    }
}
