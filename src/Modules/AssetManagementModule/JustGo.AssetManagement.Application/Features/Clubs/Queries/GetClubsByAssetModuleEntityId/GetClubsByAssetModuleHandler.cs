using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetLeaseId;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.Clubs.Queries.GetClubsByAssetModuleEntityId;


public class GetClubsByAssetModuleHandler : IRequestHandler<GetClubsByAssetModuleQuery, List<ClubMemberDTO>>
{

    private readonly LazyService<IReadRepository<ClubMemberDTO>> _readRepository;
    private readonly IMediator _mediator;

    public GetClubsByAssetModuleHandler(LazyService<IReadRepository<ClubMemberDTO>> readRepository, IMediator mediator)
    {
        _readRepository = readRepository;
        _mediator = mediator;
    }

    public async Task<List<ClubMemberDTO>> Handle( GetClubsByAssetModuleQuery request, CancellationToken cancellationToken)
    {

        var (sql, parameters) = BuildQuery(request);
        var result = await _readRepository.Value.GetListAsync( sql, cancellationToken, parameters, commandType: "text");

        return result.ToList();
    }

    private static (string Sql, DynamicParameters Parameters) BuildQuery(GetClubsByAssetModuleQuery request)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@ModuleEntityId", request.EntityId);
        parameters.Add("@ModuleId", (int)request.EntityType, dbType: DbType.Int32);

        var (tableName, subquery, columnName) = request.EntityType switch
        {
            EntityType.Transfer => (
                "Assettransferowners",
                "(SELECT TOP 1 AssetOwnershipTransferId FROM AssetOwnershipTransfers WHERE RecordGuid = @ModuleEntityId)",
                "AssetOwnershipTransferId"
            ),
            EntityType.Lease => (
                "AssetLeaseOwners",
                "(SELECT TOP 1 AssetLeaseId FROM AssetLeaseOwners WHERE RecordGuid = @ModuleEntityId)",
                "AssetLeaseId"
            ),
            EntityType.Asset => (
                "AssetOwners",
                "(SELECT TOP 1 AssetId FROM AssetOwners WHERE RecordGuid = @ModuleEntityId)",
                "AssetId"
            ),
            _ => (string.Empty, string.Empty, string.Empty)
        };

        if (string.IsNullOrEmpty(tableName))
        {
            return (string.Empty, parameters);
        }

        var sql = $$"""
            DECLARE @AssetModuleEntityId INT = {{subquery}};
            
            SELECT DISTINCT 
                D.SyncGuid AS ClubGuid,
                CD.ClubName,
                CD.Location AS Image,
                CD.ClubId,
                CD.DocId,
                CD.ClubType
            FROM ClubMemberroles CMR
            INNER JOIN [User] U ON U.UserId = CMR.UserId
            INNER JOIN Clubs_default CD ON CD.DocId = CMR.ClubDocId
            INNER JOIN Document D ON D.DocId = CD.DocId
            INNER JOIN AssetTransactionFee ATF ON ATF.FeeLinkId= D.Docid AND ATF.Type = @ModuleId
            INNER JOIN {{tableName}} AO 
                ON AO.OwnerId = CMR.UserId 
                AND AO.{{columnName}} = @AssetModuleEntityId 
                AND AO.OwnerType = 2
            """;

        return (sql, parameters);
    }
}