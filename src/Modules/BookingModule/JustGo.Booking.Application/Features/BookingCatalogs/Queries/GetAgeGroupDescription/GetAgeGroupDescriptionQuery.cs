using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroupDescription;

public class GetAgeGroupDescriptionQuery(int id) : IRequest<DescriptionDto?>
{
    public int Id { get; } = id;
}