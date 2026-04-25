using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Organisation.Domain.Entities;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetOrganizationHierarchyByMemberSyncGuid;

public class GetOrganizationHierarchyByMemberSyncGuidQuery : IRequest<List<HierarchyType>>
{
    public GetOrganizationHierarchyByMemberSyncGuidQuery(Guid syncGuid)
    {
        SyncGuid = syncGuid;
    }

    public Guid SyncGuid { get; set; }
}
