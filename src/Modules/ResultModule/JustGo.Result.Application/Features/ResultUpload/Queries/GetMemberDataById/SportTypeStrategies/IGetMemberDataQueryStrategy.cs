using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetMemberDataById.SportTypeStrategies;

public interface IGetMemberDataQueryStrategy
{
    Task<Result<object>> ExecuteAsync(GetMemberDataByIdQuery request, CancellationToken cancellationToken = default);
}