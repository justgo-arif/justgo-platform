using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetBasicClubDetails;

public class GetBasicClubDetailBySyncGuidQuery(Guid syncGuid) : IRequest<BasicClubDetailDto?>
{
    public Guid SyncGuid { get; } = syncGuid;
}