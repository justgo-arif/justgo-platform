using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Application.DTOs;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetMenuOrganisation;

public class GetMenuOrganisationQuery : IRequest<List<EntityExtensionOrganisationDto>>
{
    public GetMenuOrganisationQuery(Guid id)
    {
        UserGuid = id;
    }

    public Guid UserGuid { get; set; }
}
