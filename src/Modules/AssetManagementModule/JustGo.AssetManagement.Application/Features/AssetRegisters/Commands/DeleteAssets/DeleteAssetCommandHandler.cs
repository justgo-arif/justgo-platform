using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetAssetStatuses;
using JustGo.AssetManagement.Application.Features.Notes.Commands.DeleteNoteCommands;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using Serilog;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;

namespace JustGo.AssetManagement.Application.Features.Notes.Commands.DeleteAssetCommands
{
    public class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand, string>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public DeleteAssetCommandHandler(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IWriteRepositoryFactory writeRepository,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<string> Handle(DeleteAssetCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await DeleteAsset(command, currentUserId, cancellationToken);
        }



        private async Task<bool> PermissionCheck(string AssetRegisterId)
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
                       ConditionJoiner = ""

                   }
                }
            });

            if (IsInMyAssetList.TotalCount > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<string> DeleteAsset(DeleteAssetCommand command, int currentUserId, CancellationToken cancellationToken)
        {

            var permissionCheckResult = await PermissionCheck(command.AssetRegisterId);
            if (!permissionCheckResult)
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            }


            var dbTransaction = await _unitOfWork.BeginTransactionAsync(); 

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetRegisterId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                   select AssetTypeId from AssetRegisters where RecordGuid = @RecordGuid
               )", cancellationToken, queryParameters, null, "text");


            var asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                   .GetAsync($@"Select * from AssetRegisters where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            var assetOwners = (await _readRepository.GetLazyRepository<AssetOwner>().Value
                     .GetListAsync($@"Select * from AssetOwners where AssetId = (
                                select AssetId from AssetRegisters where RecordGuid = @RecordGuid
                        )", cancellationToken, queryParameters, null, "text")).ToList();

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);

            queryParameters = new DynamicParameters();
            queryParameters.Add("@currentUserId", currentUserId);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
                        .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = @currentUserId", cancellationToken, queryParameters, null, "text");

            if (!userGroups.Any(r => typeConfig.Permission.Delete.Contains(r.Value)))
            {
                return null;
            }

            var draftStatusId = await _mediator.Send(new GetAssetStatusIdQuery()
            {
                Status = AssetStatusType.Draft,
            });

            var qparams = new DynamicParameters();
            qparams.Add("@RecordGuid", command.AssetRegisterId);
            qparams.Add("@draftStatusId", draftStatusId);
            
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetLicenses  Where AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetCredentials  Where AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetOwners  Where AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetTagLink  Where AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetImages  Where AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetOwnerships  Where EntityType = 1 and AssetId = (Select AssetId from AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId)", qparams, dbTransaction, "text");
            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"delete AssetRegisters  Where RecordGuid = @RecordGuid and StatusId = @draftStatusId", qparams, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);

            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.General.Value,
                AuditScheme.AssetManagement.General.Deleted.Value,
               currentUserId,
               asset.AssetId,
               LogEntityType.Asset,
               asset.AssetId,
               AuditScheme.AssetManagement.General.Deleted.Name,
               "Asset deleted;" + JsonConvert.SerializeObject(new
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
                    AssetOwners = assetOwners
                })
              );

            return command.AssetRegisterId;
            
        }

    }
}
