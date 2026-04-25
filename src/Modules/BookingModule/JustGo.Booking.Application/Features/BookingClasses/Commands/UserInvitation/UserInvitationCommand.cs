using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UserInvitation;

public class UserInvitationCommand(Guid inviteId) : IRequest<int>
{
    public Guid InviteId { get; } = inviteId;
}
