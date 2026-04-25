using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.VerifyMFALogin;

public class VerifyMFALoginQueryHandler : IRequestHandler<VerifyMFALoginQuery, User>
{
    private readonly LazyService<IReadRepository<User>> _readRepository;
    private readonly IUtilityService _utilityService;
    public VerifyMFALoginQueryHandler(LazyService<IReadRepository<User>> readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }
    public async Task<User> Handle(VerifyMFALoginQuery request, CancellationToken cancellationToken)
    {
        string sql = @"Select * from [User] 
                    where LoginId=@LoginId and Password=@Password";
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@LoginId", request.UserName);
        queryParameters.Add("@Password", _utilityService.Encrypt(request.Password));

        return await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
    }
}
