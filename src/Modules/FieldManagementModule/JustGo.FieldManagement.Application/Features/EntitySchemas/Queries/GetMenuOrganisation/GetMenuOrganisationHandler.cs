using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.DTOs;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetMenuOrganisation;

public class GetMenuOrganisationHandler : IRequestHandler<GetMenuOrganisationQuery, List<EntityExtensionOrganisationDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetMenuOrganisationHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<EntityExtensionOrganisationDto>> Handle(GetMenuOrganisationQuery request, CancellationToken cancellationToken)
    {
        var result = await GetMenuOrganisationsAsync(request, cancellationToken);

        return result
            .GroupBy(x => new { x.OwnerId, x.OwnerName, x.OwnerImage, x.OwnerType })
            .Select(g => new EntityExtensionOrganisationDto
            {
                OwnerId = g.Key.OwnerId,
                OwnerName = g.Key.OwnerName,
                OwnerImage = string.IsNullOrWhiteSpace(g.Key.OwnerImage) ? null : g.Key.OwnerId == 0 ? "/store/download?f=" + g.Key.OwnerImage + "&t=OrganizationLogo" : "/store/download?f=" + g.Key.OwnerImage + "&t=repo&p=" + g.Key.OwnerId + "&p1=&p2=2",
                OwnerType = g.Key.OwnerType,
                Items = g.Select(x => new EntityExtensionOrganisationItemDto
                {
                    ExId = x.ExId,
                    ItemId = x.ItemId,
                    ParentId = x.ParentId,
                    Name = x.Name,
                    Class = x.Class,
                    Config = x.Config,
                    FieldId = x.FieldId,
                    Sequence = x.Sequence,
                    SyncGuid = x.SyncGuid
                }).ToList()
            }).ToList();
    }

    public async Task<List<EntityExtensionOrganisation>> GetMenuOrganisationsAsync(GetMenuOrganisationQuery request, CancellationToken cancellationToken)
    {
        string sql = """
                    WITH 
                    JoinedOrg AS (
                        SELECT
                        0 OwnerId,
                        (SELECT [value] from SystemSettings where Itemkey = 'ORGANISATION.NAME') OwnerName,
                        (SELECT [value] from SystemSettings where Itemkey = 'ORGANISATION.LOGO') OwnerImage,
                        'Ngb' OwnerType
                        UNION ALL
                        SELECT CD.DocId OwnerId, CD.ClubName OwnerName, CD.[Location] OwnerImage, 'Club' OwnerType
                        FROM [User] U
                        INNER JOIN Members_Links ml on ml.docid = U.MemberDocId AND Entityparentid = 3
                        INNER JOIN Clubmembers_Default cmd on cmd.docid = ml.entityid
                        INNER JOIN Clubmembers_Links cml on cml.docid = cmd.docid AND cml.Entityparentid = 2
                        INNER JOIN Clubs_Default cd on cd.Docid = cml.entityId
                        WHERE U.UserSyncId = @UserGuid
                    ),
                    FieldForms AS (
                        SELECT DISTINCT s.OwnerId, ui.ExId, ui.ItemId, ui.ParentId, ui.[Sequence], ui.Class, ui.Config, ui.FieldId, ui.SyncGuid
                        FROM EntityExtensionUI ui
                        INNER JOIN EntityExtensionSchema s ON ui.ExId = s.ExId
                        INNER JOIN EntityExtensionUI parent_ui ON parent_ui.ExId = s.ExId AND parent_ui.ParentId = 0 AND ui.ParentId = parent_ui.ItemId
                        INNER JOIN JoinedOrg JO ON JO.OwnerId = s.OwnerId
                        WHERE s.ExtensionArea = 'Profile' AND s.ExtensionEntityId = 0 AND ui.ParentId > 0
                    )
                    SELECT JO.OwnerId, JO.OwnerName, JO.OwnerImage, JO.OwnerType,
                    FF.ExId, FF.ItemId, FF.ParentId, FF.[Sequence], FF.Class, FF.Config, FF.FieldId, FF.SyncGuid
                    FROM JoinedOrg JO
                    INNER JOIN FieldForms FF ON FF.OwnerId = JO.OwnerId;
                    """;

        return (await _readRepository.GetLazyRepository<EntityExtensionOrganisation>().Value.GetListAsync(sql, cancellationToken, new { UserGuid = request.UserGuid }, null, "text")).AsList();


    }
}
