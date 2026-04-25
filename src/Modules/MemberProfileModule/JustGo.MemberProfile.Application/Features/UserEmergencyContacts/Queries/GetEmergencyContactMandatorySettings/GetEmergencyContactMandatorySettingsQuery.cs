using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetEmergencyContactMandatorySettings
{
    // Response model used as API metadata
    public class EmergencyContactMandatorySettings
    {
        public bool EmailMandatory { get; set; }
        public bool PhoneMandatory { get; set; }
    }

    public class GetEmergencyContactMandatorySettingsQuery : IRequest<EmergencyContactMandatorySettings>
    {
    }
}