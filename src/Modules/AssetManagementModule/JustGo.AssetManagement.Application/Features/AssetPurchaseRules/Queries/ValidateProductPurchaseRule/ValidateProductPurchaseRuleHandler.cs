using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.EvaluateAssetPurchaseRule;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.ValidateProductPurchaseRule
{
    public class ValidateProductPurchaseRuleHandler 
        : IRequestHandler<ValidateProductPurchaseRuleQuery, AssetPurchaseRuleResultDTO>
    {
        private readonly IReadRepositoryFactory _readDb;

        private readonly IUtilityService _utilityService;
        private IMediator _mediator;

        public ValidateProductPurchaseRuleHandler(
            LazyService<IReadRepositoryFactory> readRepository,
            IUtilityService utilityService,
            IMediator mediator)
        {
            _readDb = readRepository.Value;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        public async Task<AssetPurchaseRuleResultDTO> Handle(
            ValidateProductPurchaseRuleQuery request, 
            CancellationToken cancellationToken)
        {
            int assetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { request.AssetRegisterId.ToString() } }))[0];
            int productDocId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { request.ProductId.ToString() } }))[0];

            AssetPurchaseRuleResultDTO  assetPurchaseRuleResultDTO= await HasActiveMembershipForOwner(assetId, cancellationToken);
            if (!assetPurchaseRuleResultDTO.IsEligible)
            {
                assetPurchaseRuleResultDTO.Reason = "Owner does not have an active membership.";
                return assetPurchaseRuleResultDTO;
            }
            var evalResult = await _mediator.Send(
                    new EvaluateAssetPurchaseRuleQuery(
                        productDocId,
                        1,
                        assetId
                    ),
                    cancellationToken);

            return new AssetPurchaseRuleResultDTO
            {
                IsEligible = evalResult != null ? evalResult.IsEligible:true,
                Reason = evalResult != null ? evalResult.Reason:""
            };
        }

        private async Task<AssetPurchaseRuleResultDTO> HasActiveMembershipForOwner(int assetId, CancellationToken cancellationToken)
        {
            string sql = @"declare @LicenseOwner nvarchar(max) =''

                         SET @LicenseOwner =  (SELECT STRING_AGG(value, ', ') AS license_owner
                         						FROM Assettypes
                         						CROSS APPLY OPENJSON(assettypeconfig, '$.LicenseOwners')
                         						where Assettypeid = 1 )
                         
                          IF (ISNULL(@LicenseOwner,'') !='')
						  BEGIN
                          IF  EXISTS (SELECT h.entityid
                          FROM hierarchylinks hl
                          INNER JOIN hierarchies h ON h.Id = hl.HierarchyId
                          INNER JOIN AssetOwners AO ON AO.OwnerId = hl.UserId AND AO.AssetId = @AssetId
                          WHERE NOT EXISTS (
                              SELECT 1
                              FROM AssetOwners ao_check
                              WHERE ao_check.AssetId = @AssetId
                              AND NOT EXISTS (
                                  SELECT 1
                                  FROM UserMemberships UM
                             		INNER JOIN Hierarchies on Hierarchies.EntityId = ISNULL(UM.LicenceOwner, 0)
                             		INNER JOIN hierarchytypes hierarchytypes on hierarchytypes.Id = Hierarchies.HierarchyTypeId 
                         			AND hierarchytypes.HierarchyTypeName in ((SELECT TRIM(value) FROM string_split(@LicenseOwner, ','))) 
                                  WHERE UM.StatusId = 62
                                  AND UM.UserId = ao_check.OwnerId
                              ) )
                          )
                          BEGIN
                         	SELECT 1 IsEligible
                          END
                          ELSE
                         	SELECT 0 IsEligible
						 END
						 ELSE
							SELECT 1 IsEligible 
                                                 ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", assetId, dbType: DbType.Int32);
            var result = await _readDb.GetLazyRepository<AssetPurchaseRuleResultDTO>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            return (AssetPurchaseRuleResultDTO)result;
        }

    }
}