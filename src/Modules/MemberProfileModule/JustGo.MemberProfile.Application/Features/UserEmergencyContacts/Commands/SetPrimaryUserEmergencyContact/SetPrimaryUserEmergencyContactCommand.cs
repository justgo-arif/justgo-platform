using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.SetPrimaryUserEmergencyContact
{
    public class SetPrimaryUserEmergencyContactCommand : IRequest<int>
    {
        public SetPrimaryUserEmergencyContactCommand(int id)
        {
            ContactId = id;
        }
        public int ContactId { get; set; }
    }
}
