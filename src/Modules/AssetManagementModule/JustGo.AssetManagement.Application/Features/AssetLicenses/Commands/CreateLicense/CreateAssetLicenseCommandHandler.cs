using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using Newtonsoft.Json;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using JustGo.AssetManagement.Application.Features.Common.Helpers;
using System.Data.Common;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;


namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.CreateLicense
{
    public class CreateAssetLicenseCommandHandler : IRequestHandler<CreateAssetLicenseCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUtilityService _utilityService;
        private readonly IHybridCacheService _cache;
        public CreateAssetLicenseCommandHandler(
          IMediator mediator,
          IReadRepositoryFactory readRepository,
          IWriteRepositoryFactory writeRepository,
          IUtilityService utilityService, IHybridCacheService cache)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _utilityService = utilityService;
            _cache = cache;
        }

        public async Task<bool> Handle(CreateAssetLicenseCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            foreach (var item in command.assetLicenses)
            {

                int licenseDocId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { item.LicenseId.ToString() } }))[0];
                var licenseDate = await GetLicenseStartAndExpiryDate(licenseDocId, cancellationToken);

                AssetLicense assetLicense = new AssetLicense();
                assetLicense.AssetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { item.AssetRegisterId.ToString() } }))[0];
                assetLicense.ProductId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { item.ProductId.ToString() } }))[0];
                assetLicense.StartDate = licenseDate.StartDate;
                assetLicense.EndDate = licenseDate.EndDate;
                assetLicense.StatusId = await _mediator.Send(new GetLicenseStatusIdQuery() { Status = LicenseStatusType.PendingPayment });


                var licenseTypeSQL = @"select 
                                        Top 1
                                        LicenseType LicenseType
                                        from 
                                        AssetTypesLicenseLink
                                        where 
                                        LicenseDocId = @LicenseDocId ";



                var parameters = new DynamicParameters();
                parameters.Add("@LicenseDocId", licenseDocId, DbType.Int32);

                var resultLicenseType = await _readRepository.GetLazyRepository<dynamic>().Value
                    .GetAsync(licenseTypeSQL, cancellationToken, parameters, null, "text");


                assetLicense.LicenseType = (LicenseType)resultLicenseType.LicenseType;


                if (!await ValidateAssetLicenseAsync(assetLicense.AssetId, licenseDocId,assetLicense.ProductId, cancellationToken)) continue;


                assetLicense.SetCreateInfo(currentUserId);
                var (sql, qparams) = SQLHelper
                    .GenerateInsertSQLWithParameters(assetLicense,
                    new string[] { "AssetLicenseId", "RecordGuid" },
                    "AssetLicenses");
                var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, null, "text");
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {assetLicense.AssetId}, @LeaseId = null, @AssetLicenseId = {result.Id}",
                                                                            cancellationToken, null, null, "text");



                CustomLog.Event(AuditScheme.AssetManagement.Value,
                    AuditScheme.AssetManagement.AssetLicense.Value,
                    AuditScheme.AssetManagement.AssetLicense.Created.Value,
                   currentUserId,
                   assetLicense.AssetLicenseId,
                   LogEntityType.Asset,
                   assetLicense.AssetId,
                   AuditScheme.AssetManagement.AssetLicense.Created.Name,
                   "Asset License Created;" + JsonConvert.SerializeObject(command)
                  );

                var queryParameters = new DynamicParameters();
                queryParameters.Add("@AssetId", assetLicense.AssetId);

                var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
               .GetAsync($@"Select * from AssetRegisters where AssetId = @AssetId", cancellationToken, queryParameters, null, "text");

                if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
                {
                    await _mediator.Send(new AssetStateAllocationCommand()
                    {
                        AssetRegisterId = asset.RecordGuid
                    });

                }

            }

            await _cache.RemoveByTagAsync("policy_ext_asset_allow_ui_license_detail");
            return true;
        }
        
        public async Task<LicensePreparationInfo> GetLicenseStartAndExpiryDate(int licenseDocId, CancellationToken cancellationToken)
        {
            string sql = @"BEGIN
                            EXEC [MEMBERSHIP_STARTEXPIRY_GET] 
                                @LicenseDocId, 
                                @PreparedStartDate OUTPUT, 
                                @PreparedEndDate OUTPUT, 
                                0, 
                                0;
                           END";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LicenseDocId", licenseDocId, dbType: DbType.Int32);
            queryParameters.Add("@PreparedStartDate", dbType: DbType.DateTime, direction: ParameterDirection.Output);
            queryParameters.Add("@PreparedEndDate", dbType: DbType.DateTime, direction: ParameterDirection.Output);
            var data = await _readRepository.GetLazyRepository<dynamic>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            // Retrieve output values
            DateTime? startDate = queryParameters.Get<DateTime?>("@PreparedStartDate");
            DateTime? endDate = queryParameters.Get<DateTime?>("@PreparedEndDate");

            return new LicensePreparationInfo { StartDate = (DateTime)startDate, EndDate = (DateTime)endDate };
        }

        private async Task<bool> ValidateAssetLicenseAsync(int assetId, int licenseDocId,int productDocId, CancellationToken cancellationToken)
        {
            string sql = @"DECLARE @Result BIT;
        DECLARE @LicenseType int =  0
        DECLARE @AssetTypesId int =  0
        DECLARE @MaximumAllownedLicense int =  0

        SELECT @LicenseType = LicenseType,@AssetTypesId = AssetTypeId FROM AssetTypesLicenseLink   WHERE LicenseDocId = @LicenseDocId
        set @MaximumAllownedLicense = (SELECT top 1 JSON_VALUE(AssetRegistrationConfig, '$.Steps.License.Config.Requirements.MaximumCoreAllowed') AS MaximumCoreAllowed FROM   assettypes where AssetTypeId = @AssetTypesId)
		declare @RenewalWindow int = (select RenewalWindow from License_Default where docid = @LicenseDocId)
        IF EXISTS (select 1 from AssetLicenses A inner join AssetStatus S on S.AssetStatusId = A.StatusId AND S.Type = 4 
                    And S.Name NOT IN  ('Expired','Suspended')  where AssetId = @AssetId AND ProductId = @ProductId
                    AND NOT EXISTS (  select 2 from AssetLicenses A1 inner join AssetStatus S1 on S.AssetStatusId = A1.StatusId AND S1.Type = 4  And S1.Name  IN  ('Active') AND A1.ProductId = A.ProductId AND A1.AssetId = A.AssetId
                    where  A.EndDate <= GETDATE()+@RenewalWindow)
                    )
		BEGIN
			SELECT 0 AS IsAllowed;
			return
		END

        ;WITH CTE_AssetStatus AS (
            SELECT AssetStatusId 
            FROM AssetStatus 
            WHERE Type = 4 AND Name NOT IN ('Expired', 'Suspended')
        )
        SELECT @Result = 
            CASE 
                WHEN @MaximumAllownedLicense = 0 THEN 1
                WHEN COUNT(al.AssetId) < @MaximumAllownedLicense THEN 1
                ELSE 0
            END
        FROM AssetLicenses al
        INNER JOIN License_Links ll ON ll.EntityId = al.ProductId
        INNER JOIN AssetTypesLicenseLink ATL ON ATL.LicenseDocId = ll.DocId AND ATL.LicenseType = @LicenseType
        INNER JOIN CTE_AssetStatus cte ON cte.AssetStatusId = al.StatusId
        WHERE al.AssetId = @AssetId;

        SELECT @Result AS IsAllowed;";
            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", assetId, DbType.Int32);
            parameters.Add("@LicenseDocId", licenseDocId, DbType.Int32);
            parameters.Add("@ProductId", productDocId, DbType.Int32);

            var result = await _readRepository.GetLazyRepository<dynamic>().Value
                .GetAsync(sql, cancellationToken, parameters, null, "text");

            bool isAllowed = true;
            if (result != null)
            {
                isAllowed = Convert.ToBoolean(result.IsAllowed);
            }

            return isAllowed;
        }




    }
}
