using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplineDescription;

public class GetDisciplineDescriptionQuery(Guid id) : IRequest<DescriptionDto?>
{
    public Guid Id { get; } = id;
}