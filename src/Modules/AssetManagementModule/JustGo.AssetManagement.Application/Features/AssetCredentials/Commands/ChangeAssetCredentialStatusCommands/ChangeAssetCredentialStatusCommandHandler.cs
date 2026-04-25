using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.Authentication.Infrastructure.Logging;
using static Dapper.SqlMapper;
using System.Data.Common;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using JustGo.AssetManagement.Application.Features.Common.Helpers;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetCredentialStatusCommands
{
    public class ChangeAssetCredentialStatusCommandHandler : IRequestHandler<ChangeAssetCredentialStatusCommand, string>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeAssetCredentialStatusCommandHandler(
            IMediator mediator,
            IWriteRepositoryFactory writeRepository,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService,
            IUnitOfWork unitOfWork
            )
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> Handle(ChangeAssetCredentialStatusCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await ChangeStatus(command, currentUserId, cancellationToken);
        }


        private async Task<bool> PermissionCheck(string AssetRegisterId)
        {

            var IsInAssetList = await _mediator.Send(new GetAssetsQuery()
            {
                PageNumber = 1,
                PageSize = 1,
                SearchItems = new List<SearchSegmentDTO>() {
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "AssetRecordGuid",
                       FieldId = "",
                       Operator = "equals",
                       Value = AssetRegisterId,
                       ConditionJoiner = ""

                   }
                }
            });

            if (IsInAssetList.TotalCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> ChangeStatus(ChangeAssetCredentialStatusCommand command, int currentUserId, CancellationToken cancellationToken)
        {
            int statusId = await _mediator.Send(new GetCredentialStatusIdQuery() { Status = command.Status });

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetCredentialId);

            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
           .GetAsync($@"Select * from AssetRegisters where AssetId = (
                                Select AssetId from AssetCredentials where RecordGuid = @RecordGuid
                            )", cancellationToken, queryParameters, null, "text");

            if(!(await PermissionCheck(asset.RecordGuid)))
            {
                return null;
            }

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                            select AssetTypeId from AssetRegisters where AssetId = (
                                Select AssetId from AssetCredentials where RecordGuid = @RecordGuid
                            )
                        )", cancellationToken, queryParameters, null, "text");

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
                        .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = {currentUserId}", cancellationToken, null, null, "text");

            if (!userGroups.Any(r => typeConfig.Permission.Update.Contains(r.Value)))
            {
                return null;
            }

            var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var assetCredential = await _readRepository.GetLazyRepository<AssetCredential>().Value
                    .GetAsync($@"select * from AssetCredentials Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, dbTransaction, "text");

            assetCredential.SetUpdateInfo(currentUserId);
            assetCredential.StatusId = statusId;

            var (sql, qparams) = SQLHelper
                .GenerateUpdateSQLWithParameters(assetCredential, "RecordGuid",
                new string[] { "AssetCredentialId", "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                "AssetCredentials");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, dbTransaction, "text");

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@currentUserId", currentUserId);
            dynamicParameters.Add("@CredentialAssetId", asset.AssetId);
            await _writeRepository.GetLazyRepository<object>().Value
           .ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @CredentialAssetId, @LeaseId = null, @AssetLicenseId = null",
            cancellationToken, dynamicParameters, dbTransaction, "text");


            await _unitOfWork.CommitAsync(dbTransaction);


            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.AssetCredential.Value,
                AuditScheme.AssetManagement.AssetCredential.StatusChanged.Value,
               currentUserId,
               assetCredential.AssetCredentialId,
               LogEntityType.Asset,
               assetCredential.AssetId,
               AuditScheme.AssetManagement.AssetCredential.StatusChanged.Name,
               "Asset Credential Status Changed;" + JsonConvert.SerializeObject(command)
              );

            if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
            {
                await _mediator.Send(new AssetStateAllocationCommand()
                {
                    AssetRegisterId = asset.RecordGuid
                });

            }

            return command.AssetCredentialId;
            

            
        }

    }
}
