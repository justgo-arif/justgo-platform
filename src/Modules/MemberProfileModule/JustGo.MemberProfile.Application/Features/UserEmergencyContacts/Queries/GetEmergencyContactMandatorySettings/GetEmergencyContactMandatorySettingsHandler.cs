using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetEmergencyContactMandatorySettings
{
    public class GetEmergencyContactMandatorySettingsHandler
        : IRequestHandler<GetEmergencyContactMandatorySettingsQuery, EmergencyContactMandatorySettings>
    {
        private readonly LazyService<IReadRepository<EmergencyContactMandatorySettings>> _readRepository;

        public GetEmergencyContactMandatorySettingsHandler(
            LazyService<IReadRepository<EmergencyContactMandatorySettings>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<EmergencyContactMandatorySettings> Handle(GetEmergencyContactMandatorySettingsQuery request, CancellationToken cancellationToken)
        {
            const string sql = """
                
                
                SELECT
                    EmailMandatory =
                        ISNULL(MAX(CASE WHEN ItemKey LIKE '%ORGANISATION.EMERGENCY_CONTACT_EMAIL_MANDATORY%'
                                        THEN CASE WHEN LOWER([Value]) = 'true' THEN 1 ELSE 0 END END), 0),
                    PhoneMandatory =
                        ISNULL(MAX(CASE WHEN ItemKey LIKE '%ORGANISATION.EMERGENCY_CONTACT_PHONE_MANDATORY%'
                                        THEN CASE WHEN LOWER([Value]) = 'true' THEN 1 ELSE 0 END END), 0)
                FROM dbo.SystemSettings;
                
                """;

            var list = await _readRepository.Value.GetListAsync(
                sql,
                cancellationToken,
                dbTransaction: null,
                commandType: "text");

            return list.FirstOrDefault() ?? new EmergencyContactMandatorySettings
            {
                EmailMandatory = false,
                PhoneMandatory = false
            };
        }
    }
}