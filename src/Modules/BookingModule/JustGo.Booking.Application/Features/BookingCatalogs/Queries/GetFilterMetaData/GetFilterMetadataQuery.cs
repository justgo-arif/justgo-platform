using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetFilterMetaData;

public class GetFilterMetadataQuery(Guid syncGuid,Guid? webletGuid) : IRequest<FilterMetadataDto?>
{
    public Guid SyncGuid { get; } = syncGuid;
    public Guid? WebletGuid { get; } = webletGuid;
}