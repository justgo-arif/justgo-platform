using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Commands.CancelCourseBooking
{
    public class CancelCourseBookingCommand : IRequest<int>
    {
        public CancelCourseBookingCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }
}
