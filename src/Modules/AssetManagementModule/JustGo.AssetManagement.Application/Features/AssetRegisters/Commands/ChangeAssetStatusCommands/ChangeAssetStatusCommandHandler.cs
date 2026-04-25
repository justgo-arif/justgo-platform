using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetAssetStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Logging;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetUICacheInvalidateCommands;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetStatusCommandHandler : IRequestHandler<ChangeAssetStatusCommand, string>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;

        public ChangeAssetStatusCommandHandler(
            IMediator mediator,
            IWriteRepositoryFactory writeRepository,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;

        }

        public async Task<string> Handle(ChangeAssetStatusCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var result = await ChangeStatus(command, currentUserId, cancellationToken);

            if (result != null)
            {
                await _mediator.Send(new AssetUICacheInvalidateCommand());

            }
            return result;
        }


        private async Task<bool> EditPermissionCheck(string AssetRegisterId)
        {
            var IsInMyAssetList = await _mediator.Send(new GetMyAssetsQuery()
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
                       ConditionJoiner = "and"

                   },
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseIn",
                       FieldId = "",
                       Operator = "not equals",
                       Value = "1",
                       ConditionJoiner = ""

                   }
                }
            });

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
                       ConditionJoiner = "and"

                   },
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseIn",
                       FieldId = "",
                       Operator = "not equals",
                       Value = "1",
                       ConditionJoiner = ""

                   }
                }
            });

            if (IsInAssetList.TotalCount > 0 || IsInMyAssetList.TotalCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<string> ChangeStatus(ChangeAssetStatusCommand command, int currentUserId, CancellationToken cancellationToken)
        {


            var permissionCheckResult = await EditPermissionCheck(command.AssetRegisterId);
            if (!permissionCheckResult)
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            }


            int statusId = await _mediator.Send(new GetAssetStatusIdQuery() { Status = command.Status });

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetRegisterId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                   select AssetTypeId from AssetRegisters where RecordGuid = @RecordGuid
               )", cancellationToken, queryParameters, null, "text");

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);


            queryParameters.Add("@currentUserId", currentUserId);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
                        .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = @currentUserId", cancellationToken, queryParameters, null, "text");

            if (!userGroups.Any(r => typeConfig.Permission.Update.Contains(r.Value)))
            {
                return null;
            }

            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                    .GetAsync($@"select * from AssetRegisters Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");


            if (asset.StatusId == 7 && statusId != 7)
            {
                var duplicate = await _mediator.Send(new GetDuplicateAssetQuery()
                {
                    AssetName = asset.AssetName,
                });

                bool isDuplicate = duplicate != null;

                if (isDuplicate)
                {
                    throw new ConflictException("You cannot change state of this " + assetType.TypeName + " because another active " + assetType.TypeName + " with the same name exists ("+ assetType.TypeName +" ID: " + duplicate.AssetReference + "). To proceed, update this "+ assetType.TypeName +"’s name and try again.");
                }

            }

            asset.SetUpdateInfo(currentUserId);
            asset.StatusId = statusId;

            if(command.Status == AssetStatusType.Inactive || 
               command.Status == AssetStatusType.Suspended ||
               command.Status == AssetStatusType.Archived)
            {
                var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(asset, "RecordGuid",
                    new string[] { "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                    "AssetRegisters");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, null, "text");

                CustomLog.Event(AuditScheme.AssetManagement.Value,
                                AuditScheme.AssetManagement.StatusChanged.Value,
                                AuditScheme.AssetManagement.StatusChanged.Value,
                               currentUserId,
                               asset.AssetId,
                               LogEntityType.Asset,
                               asset.AssetId,
                               AuditScheme.AssetManagement.StatusChanged.Name,
                               "Asset status changed;" + JsonConvert.SerializeObject(new
                               {
                                   asset.AssetId,
                                   asset.AssetCategoryId,
                                   asset.AssetName,
                                   asset.AssetTypeId,
                                   asset.Address1,
                                   asset.Address2,
                                   asset.ManufactureDate,
                                   asset.Brand,
                                   asset.SerialNo,
                                   asset.Group,
                                   asset.AssetValue,
                                   asset.AssetConfig,
                                   asset.Country,
                                   asset.Town,
                                   asset.County,
                                   asset.PostCode,
                                   asset.Barcode,
                                   asset.StatusId
                               })
                              );


                if (command.Status == AssetStatusType.Suspended)
                {
                    await _mediator.Send(new SendAssetEmailCommand
                    {
                        MessageScheme = "Asset/Asset - On Suspended",
                        Argument = "",
                        ForEntityId = asset.AssetId,
                        TypeEntityId = asset.AssetId,
                        InvokeUserId = currentUserId,
                        OwnerId = 0,

                    }, cancellationToken);

                }
                else if (command.Status == AssetStatusType.Inactive)
                {
                    await _mediator.Send(new SendAssetEmailCommand
                    {
                        MessageScheme = "Asset/Asset - On Inactive",
                        Argument = "",
                        ForEntityId = asset.AssetId,
                        TypeEntityId = asset.AssetId,
                        InvokeUserId = currentUserId,
                        OwnerId = 0,
                    }, cancellationToken);

                }

                return command.AssetRegisterId;
            }
            else if (command.Status == AssetStatusType.PendingAction)
            {
                var actionsRequired = await _readRepository.GetLazyRepository<CountDTO>().Value
                    .GetAsync($@"exec GetRequiredActionCount 
                        @AssetRegisterId = @RecordGuid,
                        @StatusType = 1"
                       ,cancellationToken, queryParameters, null, "text");

                if (actionsRequired.TotalRowCount > 0)
                {
                    var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(asset, "RecordGuid",
                    new string[] { "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                    "AssetRegisters");

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, null, "text");

                    CustomLog.Event(AuditScheme.AssetManagement.Value,
                                    AuditScheme.AssetManagement.StatusChanged.Value,
                                    AuditScheme.AssetManagement.StatusChanged.Value,
                                   currentUserId,
                                   asset.AssetId,
                                   LogEntityType.Asset,
                                   asset.AssetId,
                                   AuditScheme.AssetManagement.StatusChanged.Name,
                                   "Asset status changed;" + JsonConvert.SerializeObject(new
                                   {
                                       asset.AssetId,
                                       asset.AssetCategoryId,
                                       asset.AssetName,
                                       asset.AssetTypeId,
                                       asset.Address1,
                                       asset.Address2,
                                       asset.ManufactureDate,
                                       asset.Brand,
                                       asset.SerialNo,
                                       asset.Group,
                                       asset.AssetValue,
                                       asset.AssetConfig,
                                       asset.Country,
                                       asset.Town,
                                       asset.County,
                                       asset.PostCode,
                                       asset.Barcode,
                                       asset.StatusId
                                   })
                                  );

                    return command.AssetRegisterId;
                }
                else {

                    return null;
                
                }

            }
            else
            {

                return null;

            }

            
        }

    }
}
