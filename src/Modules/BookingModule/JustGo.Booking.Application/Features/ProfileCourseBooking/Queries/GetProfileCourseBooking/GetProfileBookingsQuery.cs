using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.ProfileBookingDtos;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBooking
{

    public class GetProfileBookingsQuery : IRequest<List<ProfileCourseBookingGroupDto>>
    {
        public GetProfileBookingsQuery(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}
