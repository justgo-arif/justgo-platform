using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.ValidateMandatoryMFAByUserId;

public class ValidateMFAMandatoryUserQueryHandler : IRequestHandler<ValidateMFAMandatoryUserQuery, bool>
{
    private readonly LazyService<IReadRepository<SystemSettings>> _readRepository;
    public ValidateMFAMandatoryUserQueryHandler(LazyService<IReadRepository<SystemSettings>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<bool> Handle(ValidateMFAMandatoryUserQuery request, CancellationToken cancellationToken)
    {
        string sql = @"
                    IF ( (select len(Value) from SystemSettings where ItemKey ='SYSTEM.MFA.MANDATORYGROUP') > 0 and EXISTS (
                        select 1 from [Group]  inner join [GroupMembers] on [Group].GroupId = [GroupMembers].GroupId and [GroupMembers].UserId = @UserId
                        inner join [User] u on u.Userid = GroupMembers.UserId and u.LoginId!='admin'
                        where [Group].Name in (select s from SplitString((select Value from SystemSettings where ItemKey ='SYSTEM.MFA.MANDATORYGROUP'),',')) )
						)
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM [UserMFA] WHERE   UserId = @UserId and (ISNULL(EnableAuthenticatorApp,0) =1 oR ISNULL(EnableWhatsapp,0) = 1 or isnull(BypassForceSetup,0) = 1)  )
                            BEGIN
                                SELECT cast(1 as bit) AS IsValid
                            END
                            ELSE
                            BEGIN
                                SELECT cast(0 as bit) AS IsValid
                            END
                    END
                    ELSE
                    BEGIN
                            SELECT cast(0 as bit) AS IsValid
                    END";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);
        var result = await _readRepository.Value.GetSingleAsync(sql, queryParameters, null, "text");
        return Convert.ToBoolean(result);
    }
}
