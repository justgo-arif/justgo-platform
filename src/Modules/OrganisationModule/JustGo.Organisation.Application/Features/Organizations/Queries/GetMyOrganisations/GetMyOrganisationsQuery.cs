using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetMyOrganisations;

public class GetMyOrganisationsQuery : IRequest<List<MyOrganisationDto>>
{
    public GetMyOrganisationsQuery(Guid userSyncId)
    {
        UserSyncId = userSyncId;
    }

    public Guid UserSyncId { get; set; }
}


