using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;

public class GetAdminUserMFAQueryHandler : IRequestHandler<GetAdminUserMFAQuery, UserMFA>
{
    private readonly LazyService<IReadRepository<UserMFA>> _readRepository;
    private readonly IUtilityService _utilityService;
    public GetAdminUserMFAQueryHandler(LazyService<IReadRepository<UserMFA>> readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<UserMFA> Handle(GetAdminUserMFAQuery request, CancellationToken cancellationToken)
    {
        string sql = @"DECLARE @MemberDocId INT = @memberDocIdx;
                        DECLARE @UserId INT =  (SELECT Userid FROM [User] WHERE MemberDocId = @MemberDocId);
                        -- Fetch the not allowed roles
                        DECLARE @NotAllowedRoles NVARCHAR(MAX) = (SELECT Value FROM SystemSettings WHERE [ItemKey] = 'SYSTEM.MFA.NOTALLOWEDROLE');
                        declare @TimeZone int = (select [value] from SystemSettings where ItemKey  = 'ORGANISATION.TIMEZONE');
                        declare @IsVisibleEmailMFA bit = 1;
                        declare @IsVisibleWhatsappMFA bit = 1;
                        declare @IsVisibleAuthenticatorMFA bit = 1;

                        DECLARE @TMP TABLE (Name varchar(max))

                        insert into @TMP
	                        SELECT g.Name
                        FROM [GroupMembers] m
	                        INNER JOIN [Group] g ON m.GroupId = g.GroupId
	                        WHERE UserId =  1;

                        IF EXISTS ( select * from @TMP where Name in ( select value FROM OPENJSON(@NotAllowedRoles, '$.MFA.Email.NotAllowedRoles')))
                        BEGIN
                        set @IsVisibleAuthenticatorMFA = 0;
                        END

                        IF EXISTS ( select * from @TMP where Name in ( select value FROM OPENJSON(@NotAllowedRoles, '$.MFA.WhatsApp.NotAllowedRoles')))
                        BEGIN
                        set @IsVisibleWhatsappMFA = 0;
                        END

                        IF EXISTS ( select * from @TMP where Name in ( select value FROM OPENJSON(@NotAllowedRoles, '$.MFA.AuthApp.NotAllowedRoles')))
                        BEGIN
                        set @IsVisibleAuthenticatorMFA = 0;
                        END

                        select AuthenticatorKey,EnableAuthenticatorApp,
                        EnableWhatsapp,
                        DATEADD(second,x.gm_offset,EnableWhatsappDate) as EnableWhatsappDate ,
                        x.abbreviation [WhatsAppDateTimezoneName],
                        DATEADD(second,y.gm_offset,EnableAuthenticatorAppDate ) as EnableAuthenticatorAppDate  ,
                        y.abbreviation [AppStartDateTimezoneName],
                        DATEADD(second,Z.gm_offset,EmailAuthEnableDate ) as EmailAuthEnableDate  ,
                        Z.abbreviation [EmailAuthEnableDateTimezoneName],
					
                        BackUpCode,whatsAppState,AuthenticatorAppState,WhatsAppNumber,PhoneCode,CountryCode, Email, IsEmailAuthEnabled, EmailAuthState, @IsVisibleEmailMFA AS 'IsVisibleEmailMFA', @IsVisibleAuthenticatorMFA AS 'IsVisibleAuthenticatorMFA', @IsVisibleWhatsappMFA AS 'IsVisibleWhatsappMFA'  from [userMFA] 
                        mfa inner join [User] u on u.Userid=mfa.userid
                        OUTER APPLY
                        (select top  1 gm_offset,abbreviation from Timezone where time_start <= cast(DATEDIFF(HOUR,'1970-01-01 00:00:00', mfa.EnableWhatsappDate) as bigint)*60*60
                        and zone_id=@TimeZone order by time_start desc) as X
                        OUTER APPLY
                        (select top  1 gm_offset,abbreviation from Timezone where time_start <=  cast(DATEDIFF(HOUR,'1970-01-01 00:00:00', mfa.EnableAuthenticatorAppDate) as bigint)*60*60
                        and zone_id=@TimeZone order by time_start desc) as Y
                        OUTER APPLY
                        (select top  1 gm_offset,abbreviation from Timezone where time_start <=  cast(DATEDIFF(HOUR,'1970-01-01 00:00:00', mfa.EmailAuthEnableDate) as bigint)*60*60
                        and zone_id=@TimeZone order by time_start desc) as Z
                        where u.MemberDocId = @MemberDocId";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@memberDocIdx", request.MemberDocId);

        var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");

        if (result != null)
        {
            result.EnableAuthenticatorAppDate = GetDate(result.EnableAuthenticatorAppDate);
            result.EnableWhatsappDate = GetDate(result.EnableWhatsappDate);
            result.AuthenticatorKey = string.IsNullOrWhiteSpace(result.AuthenticatorKey) ? "" : _utilityService.DecryptData(result.AuthenticatorKey.ToString());
            result.BackUpCode = string.IsNullOrWhiteSpace(result.BackUpCode) ? "" : _utilityService.DecryptData(result.BackUpCode.ToString());
            result.AuthenticatorAppState = GetByte(result.AuthenticatorAppState);
        }

        return result;
    }
    private bool GetBool(object b)
    {
        return b != DBNull.Value && (bool)b;
    }
    private DateTime GetDate(object d)
    {
        return d == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(d.ToString());
    }
    private Byte GetByte(object i)
    {
        return i == DBNull.Value || i == null ? Byte.MinValue : (Byte)i;
    }
}
