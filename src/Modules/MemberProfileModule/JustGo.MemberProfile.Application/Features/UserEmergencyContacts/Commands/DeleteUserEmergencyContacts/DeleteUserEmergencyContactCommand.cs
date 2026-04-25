using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.DeleteUserEmergencyContacts
{
    public class DeleteUserEmergencyContactCommand : IRequest<int>
    {
        public DeleteUserEmergencyContactCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}
