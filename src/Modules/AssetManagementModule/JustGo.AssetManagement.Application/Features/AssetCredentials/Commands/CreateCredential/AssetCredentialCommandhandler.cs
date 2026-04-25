using Azure.Core;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
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
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;

namespace JustGo.AssetManagement.Application.Features.AssetCredentials.Commands.CreateCredential
{
    public class AssetCredentialCommandhandler : IRequestHandler<CreateAssetCredentialCommand, int>
    {
        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IHybridCacheService _cache;
        public AssetCredentialCommandhandler(
          IMediator mediator,
          IWriteRepositoryFactory writeRepository,
          IReadRepositoryFactory readRepository,
          IUtilityService utilityService,
          IHybridCacheService cache)
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _cache = cache;
        }
        public async Task<int> Handle(CreateAssetCredentialCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            int credentialMasterId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.Document, RecordGuids = new List<string>() { command.CredentialId.ToString() } }))[0];
                //var licenseDate = await GetLicenseStartAndExpiryDate(licenseDocId, cancellationToken);

                AssetCredential assetCredential = new AssetCredential();
                assetCredential.AssetId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { command.AssetRegisterId.ToString() } }))[0];
                assetCredential.StartDate = command.Granteddate;
                assetCredential.EndDate = command.ExpiryDate;
                assetCredential.StatusId = await _mediator.Send(new GetCredentialStatusIdQuery() { Status = CredentialStatusType.AwaitingApproval });
                assetCredential.CredentialMasterDocId = credentialMasterId;
                //AssetRegister recordSaved = null;

                 var credResult = await SaveAssetCredential(assetCredential);
                 assetCredential.CredentialDocId = credResult;


            assetCredential.SetCreateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateInsertSQLWithParameters(assetCredential,
                new string[] { "AssetCredentialId", "RecordGuid" },
                "AssetCredentials");

            var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, null, "text");

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetCredentialId", result.Id);
            queryParameters.Add("@Granteddate", assetCredential.StartDate);
            queryParameters.Add("@Expirydate", assetCredential.EndDate);

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"  DECLARE @organisationTimezone INT
                                                                                        SET @organisationTimezone = (SELECT TOP 1 value FROM   SystemSettings WHERE  itemkey = 'ORGANISATION.TIMEZONE');
																						IF exists (select 1 from AssetCredentials AC inner join Credentialmaster_Default CD on CD.DocId = AC.CredentialmasterDocId 
																						where  AssetCredentialId = @AssetCredentialId and CD.Enablecredentialpayment = 1)
																						    BEGIN
																							    update  AssetCredentials  set StatusId = (select top 1  AssetStatusId from AssetStatus where Type = 3 and name = 'Pending Payment')  where  AssetCredentialId = @AssetCredentialId 
																						    END
                                                                                        ELSE
                                                                                            BEGIN
                                                                                                EXEC AssetCredentialStateUpdateByRetentionLogic @AssetCredentialId = @AssetCredentialId
                                                                                            END
                                                                                        update AssetCredentials set StartDate =@Granteddate ,EndDate = @Expirydate where AssetCredentialId = @AssetCredentialId
                                                                                        exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {assetCredential.AssetId}, @LeaseId = null, @AssetLicenseId = null",
                                                                           cancellationToken, queryParameters, null, "text");




            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetCredential.Value,
                AuditScheme.AssetManagement.AssetCredential.Created.Value,
               currentUserId,
               assetCredential.AssetCredentialId,
               LogEntityType.Asset,
               assetCredential.AssetId,
               AuditScheme.AssetManagement.AssetCredential.Created.Name,
               "Asset Credential Created;" + JsonConvert.SerializeObject(command)
              );

            queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", assetCredential.AssetId);

            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
           .GetAsync($@"Select * from AssetRegisters where AssetId = @AssetId", cancellationToken, queryParameters, null, "text");

            if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
            {
                await _mediator.Send(new AssetStateAllocationCommand()
                {
                    AssetRegisterId = asset.RecordGuid
                });

            }

            await _cache.RemoveByTagAsync("policy_ext_asset_allow_ui_credential_detail");

            return credResult;
        }

        private async Task<int> SaveAssetCredential(AssetCredential request)
        {
            int credentialDocid  = 0;
            string sql = @"declare @CredentialsType nvarchar(255)
                            declare @CredentialCode nvarchar(255)
                            declare @CredentialName nvarchar(255)
                            select @CredentialCode = Credentialcode , @CredentialsType = Credentialcategory,@CredentialName = Credentialname from Credentialmaster_Default where docid = @CredentialMasterDocId
                            --declare @CredentialDocId int 
                            EXEC CreateCredentialDocument @Name =@CredentialName, @description ='',@startDate = @StartDate,@enddate = @EndDate,@CredentialsType =@CredentialsType,@Provider='',@Credentialsource='',@userId = 1, @CredentialDocId = @CredentialDocId output
                            UPDATE MembersCredentials_Default_Original SET Isnewjourney = 1,Entitydocid = @Entitydocid, Entitytype ='Asset',Entityname = '',CredentialsType = @CredentialsType,CredentialCode =@CredentialCode ,Credentialmasterid = @CredentialMasterDocId Where Docid = @CredentialDocId
";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CredentialMasterDocId", request.CredentialMasterDocId);
            queryParameters.Add("@StartDate", request.StartDate);
            queryParameters.Add("@EndDate", request.EndDate);
            queryParameters.Add("@UserId", request.CreatedBy);
            queryParameters.Add("@Entitydocid", request.AssetId);
            queryParameters.Add("@CredentialDocId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result =  await _writeRepository.GetLazyRepository<dynamic>().Value.ExecuteAsync(sql, CancellationToken.None, queryParameters, null, "text");
            credentialDocid = (int)queryParameters.Get<int?>("@CredentialDocId");
          

            return credentialDocid;
        }

    }
}
