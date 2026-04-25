using System.Reflection;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;
using MapsterMapper;

namespace JustGo.Booking.Application.MappingProfiles
{
    public class BookingProfile : CustomMapsterProfile
    {
        public override void Register(TypeAdapterConfig config)
        {
            CreateAutoMaps(config,
                Assembly.Load("JustGo.Booking.Domain"),
                Assembly.Load("JustGo.Booking.Application"));

            config.NewConfig<SessionCoach, SessionCoachDto>().TwoWays();
            config.NewConfig<SessionOccurrence, SessionOccurrenceDto>().TwoWays();
            config.NewConfig<BookingOccurrence, BookingOccurrenceDto>().TwoWays();
        }
    }
}
