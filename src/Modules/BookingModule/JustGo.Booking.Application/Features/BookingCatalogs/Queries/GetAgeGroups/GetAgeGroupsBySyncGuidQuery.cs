using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroups;

public class GetAgeGroupsBySyncGuidQuery(Guid syncGuid, Guid? webletGuid) : IRequest<IEnumerable<AgeGroupCategoryDto>?>
{
    public Guid SyncGuid { get; } = syncGuid;
    public Guid? WebletGuid { get; } = webletGuid;
}