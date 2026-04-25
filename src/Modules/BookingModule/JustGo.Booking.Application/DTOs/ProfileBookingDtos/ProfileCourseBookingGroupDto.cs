using JustGo.Booking.Application.Features.ProfileCourseBooking.Queries;

namespace JustGo.Booking.Application.DTOs.ProfileBookingDtos
{

    public class ProfileCourseBookingGroupDto
    {
        public required string EventPeriod { get; set; }
        public List<ProfileCourseBookingDto> ProfileCourseBookings { get; set; } = new List<ProfileCourseBookingDto>();

    }
}