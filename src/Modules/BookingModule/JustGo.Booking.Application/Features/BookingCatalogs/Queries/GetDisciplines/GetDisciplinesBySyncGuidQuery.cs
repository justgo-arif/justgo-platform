using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplines;

public class GetDisciplinesBySyncGuidQuery(Guid syncGuid, Guid? webletGuid) : IRequest<IEnumerable<DisciplineCategoryDto>?>
{
    public Guid SyncGuid { get; } = syncGuid;
    public Guid? WebletGuid { get; } = webletGuid;
}