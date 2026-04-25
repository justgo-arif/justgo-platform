using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUiNew;

public class GetEntityExtensionUiNewHandler : IRequestHandler<GetEntityExtensionUiNewQuery, List<EntityExtensionUI>>
{
    private readonly string[] AllowedOwner = { "Ngb", "Club" };
    private readonly string[] AllowedArea = { "Profile", "Qualification", "Membership", "Credential", "Event", "Club", "EventMaster", "Asset" };
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUtilityService _utilityService;
    public GetEntityExtensionUiNewHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<List<EntityExtensionUI>> Handle(GetEntityExtensionUiNewQuery request, CancellationToken cancellationToken = default)
    {
        int ownerId;
        if (!AllowedOwner.Contains(request.OwnerType))
            throw new Exception("Invalid Owner Type");
        if (!AllowedArea.Contains(request.ExtensionArea))
            throw new Exception("Invalid Extension Area");
        if (request.OwnerType.Equals("Ngb"))
            ownerId = 0;
        else
        {
            ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerId, cancellationToken);
        }
        if (request.ExtensionArea.Equals("Profile") || request.ExtensionArea.Equals("Event"))
            request.ExtensionEntityId = 0;

        string sql = """
                    SELECT ui.[ExId],
                           ui.[ItemId], 
                           ui.[ParentId],
                           ui.[Sequence],
                           ui.[Class],
                           ui.[Config],
                           ui.[FieldId],
                           ui.[SyncGuid]
                    FROM [dbo].[EntityExtensionUI] ui
                    INNER JOIN [dbo].[EntityExtensionSchema] s ON ui.ExId = s.ExId
                    INNER JOIN [dbo].[EntityExtensionUI] parent_ui ON 
                        parent_ui.ExId = s.ExId 
                        AND parent_ui.ParentId = 0 
                        AND ui.ParentId = parent_ui.ItemId
                    WHERE s.OwnerType = @OwnerType
                        AND s.OwnerId = @OwnerId
                        AND s.ExtensionArea = @ExtensionArea 
                        AND s.ExtensionEntityId = @ExtensionEntityId
                        AND ui.ParentId > 0
                    ORDER BY ui.[Sequence];
                    """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@OwnerType", request.OwnerType);
        queryParameters.Add("@OwnerId", ownerId);
        queryParameters.Add("@ExtensionArea", request.ExtensionArea);
        queryParameters.Add("@ExtensionEntityId", request.ExtensionEntityId);

        var result = (await _readRepository.GetLazyRepository<EntityExtensionUI>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        return result;
    }
}
