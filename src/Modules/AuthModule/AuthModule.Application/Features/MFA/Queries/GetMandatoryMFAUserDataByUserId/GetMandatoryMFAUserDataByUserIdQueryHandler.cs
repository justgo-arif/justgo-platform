using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.GetMandatoryMFAUserDataByUserId;

public class GetMandatoryMFAUserDataByUserIdQueryHandler : IRequestHandler<GetMandatoryMFAUserDataQuery, bool>
{
    private readonly LazyService<IReadRepository<string>> _readRepository;
    public GetMandatoryMFAUserDataByUserIdQueryHandler(LazyService<IReadRepository<string>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<bool> Handle(GetMandatoryMFAUserDataQuery request, CancellationToken cancellationToken)
    {
        string sql = @"SELECT top 1 isnull(BypassForceSetup,0) as BypassForceSetup FROM [UserMFA] MFA inner join [User] U on U.Userid = MFA.UserId WHERE   u.MemberDocId = @MemberDocId";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@MemberDocId", request.MemberDocId);

        var result = await _readRepository.Value.GetSingleAsync(sql, queryParameters, null, "text");

        return Convert.ToBoolean(result);
    }
}
