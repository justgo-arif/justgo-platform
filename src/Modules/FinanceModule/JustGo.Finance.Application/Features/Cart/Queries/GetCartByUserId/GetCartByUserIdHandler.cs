using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;

namespace JustGo.Finance.Application.Features.Cart.Queries.GetCartByUserId;

public class GetCartByUserIdHandler : IRequestHandler<GetCartByUserIdQuery, int?>
{
    private readonly LazyService<IReadRepository<string>> _readRepository;

    public GetCartByUserIdHandler(LazyService<IReadRepository<string>> readRepository)
    {
        _readRepository = readRepository;
    }
    public async Task<int?> Handle(GetCartByUserIdQuery request, CancellationToken cancellationToken)
    {
        var userId = 0;
        if (request.UserGuid != Guid.Empty)
        {
            var userIdResult = await _readRepository.Value
                .GetSingleAsync(SqlQueries.SelectUserIdBySyncGuid, cancellationToken, QueryHelpers.GetGuidParams(request.UserGuid), null, "text");

            if (userIdResult is null)
            {
                return -1;
            }

            userId = (int)userIdResult;
        }
        else
        {
            userId = request.UserId;
        }


        var sql = @"SELECT DocId FROM Shoppingcart_Default WHERE OwnerUserId = @UserId";

        var queryParams = new DynamicParameters();
        queryParams.Add("UserId", userId);
        var res = await _readRepository.Value.GetSingleAsync(sql, cancellationToken, queryParams, null, "text");

        if (res is null)
            return null;

        return int.TryParse(res.ToString(), out int result) ? result : (int?)null;
    }
}
