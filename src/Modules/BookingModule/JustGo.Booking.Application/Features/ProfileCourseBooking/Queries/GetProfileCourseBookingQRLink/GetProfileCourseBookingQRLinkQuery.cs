using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.ProfileBookingDtos;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBookingQRLink
{

    public class GetProfileCourseBookingQRLinkQuery : IRequest<EventQRLink>
    {
        public Guid BookingGuid { get; }

        public GetProfileCourseBookingQRLinkQuery(Guid bookingGuid)
        {
            BookingGuid = bookingGuid;
        }
    }
}
