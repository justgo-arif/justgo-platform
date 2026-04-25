using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetValidationHelpers;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using Serilog;
using System.Data;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.EditAssets
{
    public class EditAssetCommandHandler : IRequestHandler<EditAssetCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LazyService<IReadRepository<dynamic>> _readDb;
        private readonly IUtilityService _utilityService;
        private readonly IAzureBlobFileService _azureBlobFileService;

        public EditAssetCommandHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            LazyService<IReadRepository<dynamic>> readdb,
            IUtilityService utilityService,
            IAzureBlobFileService azureBlobFileService)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _readDb = readdb;
            _utilityService = utilityService; 
            _azureBlobFileService = azureBlobFileService;
        }

        public async Task<string> Handle(EditAssetCommand command, CancellationToken cancellationToken)
        {


            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            return await SaveAsset(currentUserId, command, cancellationToken);

        }




        private async Task<bool> EditPermissionCheck(string AssetRegisterId)
        {
            var IsInMyAssetList = await _mediator.Send(new GetMyAssetsQuery()
            {
                PageNumber = 1,
                PageSize = 1,
                SearchItems = new List<SearchSegmentDTO> () {
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

        private async Task<string> SaveAsset(int currentUserId, EditAssetCommand command, CancellationToken cancellationToken)
        {

            var permissionCheckResult = await EditPermissionCheck(command.AssetRegisterId);
            if (!permissionCheckResult)
            {
                throw new ForbiddenAccessException("User is not authorized to access this resource");
            }



            var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var req = _mapper.Map<AssetRegister>(command);


            req.RecordGuid = command.AssetRegisterId;
            req.AssetCategoryId = !string.IsNullOrWhiteSpace(command.CategoryId) ? (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetCategories, RecordGuids = new List<string>() { command.CategoryId } }))[0] : null;
            req.AssetTypeId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetTypes, RecordGuids = new List<string>() { command.TypeId } }))[0];

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@AssetTypeId", req.AssetTypeId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                    .GetAsync($@"Select * from AssetTypes Where AssetTypeId = @AssetTypeId", cancellationToken, dynamicParameters, dbTransaction, "text");

            dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@currentUserId", currentUserId);

            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
             .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 
                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = @currentUserId", cancellationToken, dynamicParameters, dbTransaction, "text");

            var rssparams = new DynamicParameters();

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);

            rssparams.Add("@RecordGuid", command.AssetRegisterId);
            rssparams.Add("@EditEnabledStatuses", typeConfig.EditEnabledStatuses);

            if (typeConfig.EditEnabledStatuses.Count() > 0)
            {
            var recordToUpdate = await _readRepository.GetLazyRepository<AssetRegister>().Value
             .GetAsync($@"With StatusData as (
                             select ast.AssetStatusId Id from AssetStatus ast
                             where ast.Type = 1 and ast.Name in 
                             @EditEnabledStatuses
                           )
                          select * from 
                          AssetRegisters ar
                          inner join StatusData sd on sd.Id = ar.StatusId
                          Where RecordGuid = @RecordGuid
                         ", cancellationToken, rssparams, dbTransaction, "text");

            if (recordToUpdate == null)
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            } 
            }

            var (isValid, message) = await AssetValidator.Validate(_mediator, command, assetType, command.AssetRegisterId, userGroups.ToList());

            if (!isValid)
            {
                if (message == null)
                {
                    throw new CustomValidationException("Asset Validation Failed!");
                }
                else
                {
                    throw new CustomValidationException(message);
                }

            }

            AssetRegister recordSaved = null;

            if (!string.IsNullOrEmpty(req.RecordGuid))
            {
                req.SetUpdateInfo(currentUserId);
                var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(req, "RecordGuid",
                    new string[] { "AssetId", "AssetReference", "IssueDate", "CreatedBy", "CreatedDate", "RecordStatus", "StatusId" },
                    "AssetRegisters");
                await _writeRepository.GetLazyRepository<object>().Value
                    .ExecuteAsync(sql, cancellationToken, qparams, dbTransaction, "text");

                recordSaved = await _readRepository.GetLazyRepository<AssetRegister>().Value
                    .GetAsync($@"select * from AssetRegisters Where RecordGuid = @RecordGuid", cancellationToken, rssparams, dbTransaction, "text");

            }

            if (recordSaved != null)
            {

                await SaveTags(currentUserId, command, recordSaved.AssetId, recordSaved.AssetTypeId, dbTransaction, cancellationToken);
                await SaveOwners(currentUserId, command, recordSaved.AssetId, dbTransaction, cancellationToken);
                await SaveImages(currentUserId, command, recordSaved.AssetId, dbTransaction, cancellationToken);
            }

            await _unitOfWork.CommitAsync(dbTransaction);


            CustomLog.Event(AuditScheme.AssetManagement.Value,
                AuditScheme.AssetManagement.General.Value,
                AuditScheme.AssetManagement.General.Updated.Value,
               currentUserId,
               recordSaved.AssetId,
               LogEntityType.Asset,
               recordSaved.AssetId,
               AuditScheme.AssetManagement.General.Updated.Name,
               "Asset Updated;"+JsonConvert.SerializeObject(new
                {
                    command.CategoryId,
                    command.AssetName,
                    command.TypeId,
                    command.Address1,
                    command.Address2,
                    command.ManufactureDate,
                    command.Brand,
                    command.SerialNo,
                    command.Group,
                    command.AssetValue,
                    command.AssetConfig,
                    command.Country,
                    command.Town,
                    command.County,
                    command.PostCode,
                    command.Barcode,
                    command.AssetTags,
                    command.AssetOwners
                })
              );

            if (!command.AssetOwners.Any())
            {
                await _mediator.Send(new ChangeAssetStatusCommand()
                {
                    AssetRegisterId = command.AssetRegisterId,
                    Status = AssetStatusType.Archived,
                });
            }


            return command.AssetRegisterId;
        }

        private async Task SaveTags(int currentUserId, EditAssetCommand command, int AssetId, int AssetTypeId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", AssetId);

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync
            ("delete from AssetTagLink where AssetId = @AssetId",
            cancellationToken, queryParameters, dbTransaction, "text");


            if (command.AssetTags.Any())
            {

                string tagSqlConditions = string.Join(" or ", command.AssetTags.Select((t, i) => $@" Name = @Name{i} "));

                var tagSql = "select * from AssetTypesTag where AssetTypeId = @AssetTypeId and ( " +
                         tagSqlConditions + " ) ";

                queryParameters = new DynamicParameters();

                queryParameters.Add("@AssetTypeId", AssetTypeId);

                for (int i = 0; i < command.AssetTags.Count; i++)
                {
                    var tag = command.AssetTags[i];
                    queryParameters.Add($@"@Name{i}", tag);
                }

                var tags = await _readRepository.GetLazyRepository<AssetTypesTag>().Value
                    .GetListAsync(tagSql, cancellationToken, queryParameters, dbTransaction, "text");

                foreach (var item in command.AssetTags)
                {
                    var tag = tags.FirstOrDefault(r => r.Name == item);

                    if (tag != null)
                    {
                        var tagLink = new AssetTagLink()
                        {

                            AssetId = AssetId,
                            TagId = tag.TagId
                        };

                        tagLink.SetCreateInfo(currentUserId);

                        var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                            tagLink,
                            new string[] { "AssetTagId", "RecordGuid" },
                            "AssetTagLink");

                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                            sql, cancellationToken, qparams, dbTransaction, "text");

                    }
                    else
                    {

                        var (ttsql, ttqparams) = SQLHelper.GenerateInsertSQLWithParameters(
                            new AssetTypesTag()
                            {

                                AssetTypeId = AssetTypeId,
                                Name = item
                            },
                            new string[] { "TagId", "RecordGuid" },
                           "AssetTypesTag");

                        var tagData = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>()
                            .Value.ExecuteMultipleAsync(ttsql, ttqparams, dbTransaction, "text");

                        var tagLink = new AssetTagLink()
                        {

                            AssetId = AssetId,
                            TagId = tagData.Id
                        };

                        tagLink.SetCreateInfo(currentUserId);

                        var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                            tagLink, new string[] { "AssetTagId", "RecordGuid" }, "AssetTagLink");

                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                            sql, cancellationToken, qparams, dbTransaction, "text");

                    }
                }

            }
        }

        private async Task SaveOwners(int currentUserId, EditAssetCommand command, int AssetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {

            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", AssetId);

            List<AssetOwnerDTO> existingOwners = (List<AssetOwnerDTO>)await _readRepository.GetLazyRepository<AssetOwnerDTO>().Value
                .GetListAsync($@"select CAST(U.UserSyncId AS VARCHAR(100)) AS OwnerId from AssetOwners  AO inner join [User] U on U.UserId = AO.OwnerId  where AO.AssetId = @AssetId", 
                cancellationToken, 
                parameters, 
                dbTransaction, 
                "text");

            if (OwnersAreEqual(existingOwners, command.AssetOwners))
            {
                return;
            }

            // Delete existing owners and ownerships
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", AssetId);
            
            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync($@"delete from AssetOwners where AssetId = @AssetId", 
                cancellationToken,
                queryParameters,
                dbTransaction, 
                "text");

            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync($@"delete from AssetOwnerships
                              where AssetId = @AssetId and
                              EntityType = 1",
                cancellationToken,
                queryParameters,
                dbTransaction, 
                "text");

            // Add new owners
            foreach (var item in command.AssetOwners)
            {
                int OwnerId = 0;

                if (item.OwnerTypeId == OwnerType.Individual)
                {
                    var user = await _mediator.Send(new GetUserByUserSyncIdQuery(new Guid(item.OwnerId)), cancellationToken);
                    OwnerId = user.Userid;
                }
                else if (item.OwnerTypeId == OwnerType.Club)
                {
                    string clubSql = $@"With d as ( select DocId DocId from Document Where SyncGuid = @RecordGuid)
                        select cd.DocId as Id 
                        from Clubs_Default cd
                        inner join d on d.DocId = cd.DocId";

                    var rssparams = new DynamicParameters();
                    rssparams.Add("@RecordGuid", item.OwnerId);
                    var clubIdData = await _readRepository.GetLazyRepository<InsertedDataIdDTO>().Value
                        .GetAsync(clubSql, cancellationToken, rssparams, dbTransaction, "text");
                    OwnerId = clubIdData.Id;
                }

                var owner = new AssetOwner()
                {
                    AssetId = AssetId,
                    OwnerId = OwnerId,
                    OwnerTypeId = item.OwnerTypeId
                };

                owner.SetCreateInfo(currentUserId);

                var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                    owner, new string[] { "AssetOwnerId", "RecordGuid" }, "AssetOwners");

                var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>()
                    .Value.ExecuteMultipleAsync(
                        sql, cancellationToken, qparams, dbTransaction, "text");

                var ownerShip = new AssetOwnership()
                {
                    AssetId = AssetId,
                    OwnerId = OwnerId,
                    OwnerType = item.OwnerTypeId,
                    EntityId = result.Id,
                    EntityType = OwnershipEntityType.AssetOwner
                };

                var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                    ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sql2, cancellationToken, qparams2, dbTransaction, "text");

            }

            CustomLog.Event(AuditScheme.AssetManagement.Value,
              AuditScheme.AssetManagement.OwnershipChanged.Value,
              AuditScheme.AssetManagement.General.Updated.Value,
             currentUserId,
             AssetId,
             LogEntityType.Asset,
             AssetId,
             AuditScheme.AssetManagement.General.Updated.Name,
             "Asset Ownership Changed;" + JsonConvert.SerializeObject(new
             {
                 command.CategoryId,
                 command.AssetName,
                 command.TypeId,
                 command.Address1,
                 command.Address2,
                 command.ManufactureDate,
                 command.Brand,
                 command.SerialNo,
                 command.Group,
                 command.AssetValue,
                 command.AssetConfig,
                 command.Country,
                 command.Town,
                 command.County,
                 command.PostCode,
                 command.Barcode,
                 command.AssetTags,
                 command.AssetOwners
             })
            );
        }

        private static bool OwnersAreEqual(
            List<AssetOwnerDTO> existingOwners, 
            List<AssetOwnerDTO> newOwners)
        {
            // Early returns for edge cases
            var (existing, newList) = (existingOwners, newOwners);
            
            return (existing, newList) switch
            {
                (null, null) => true,
                (null, _) or (_, null) => false,
                _ when existing.Count != newList.Count => false,
                _ when existing.Count == 0 => true,
                _ => CompareOwnerCollections(existing, newList)
            };
        }

        private static bool CompareOwnerCollections(
            List<AssetOwnerDTO> existing, 
            List<AssetOwnerDTO> newList)
        {
            var existingNormalized = existing
                .Select(o => (o.OwnerId?.Trim() ?? "").ToUpperInvariant())
                .OrderBy(x => x);

            var newNormalized = newList
                .Select(o => (o.OwnerId?.Trim() ?? "").ToUpperInvariant())
                .OrderBy(x => x);

            return existingNormalized.SequenceEqual(newNormalized);
        }

        private async Task SaveImages(int currentUserId, EditAssetCommand command, int AssetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {

            if (command.AssetImages.Any())
            {

                Dictionary<string, int> nameDict = new Dictionary<string, int>();

                var imagesToAdd = command.AssetImages.Where(r => string.IsNullOrEmpty(r.ImageId));
                var imageToUpdate = command.AssetImages.Where(r => !string.IsNullOrEmpty(r.ImageId));

                var pastAddedImages = imageToUpdate.Any() ?
                    
                await _mediator.Send(new GetIdByGuidQuery()
                {
                    Entity = AssetTables.AssetImages,
                    RecordGuids = imageToUpdate.Select(r => r.ImageId).ToList(),
                }) : new List<int>();

                if (pastAddedImages.Any())
                {
                    DynamicParameters dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@AssetId", AssetId);
                    dynamicParameters.Add("@pastAddedImages", pastAddedImages);
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                                    @$"Delete From AssetImages Where AssetId = @AssetId
                                   and AssetImageId not in @pastAddedImages",
                                    cancellationToken, dynamicParameters, dbTransaction, "text");

                }

                foreach (var item in imageToUpdate)
                {
                    var rssparams = new DynamicParameters();
                    var image = new AssetImages()
                    {
                        RecordGuid = item.ImageId,
                        AssetId = AssetId,
                        IsPrimary = item.IsPrimary,
                    };

                    image.SetUpdateInfo(currentUserId);

                    var (sql, qparams) = SQLHelper.GenerateUpdateSQLWithParameters(
                        image, "RecordGuid", new string[] { "AssetImageId", "AssetImage", "CreatedBy", "CreatedDate", "RecordStatus" }, "AssetImages");

                    var result = await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, cancellationToken, qparams, dbTransaction, "text");

                }


                foreach (var item in imagesToAdd)
                {
                    var rssparams = new DynamicParameters();

                    string fileName = item.AssetImage.Split(new string[] { "_$_" }, StringSplitOptions.None).Last();
                    if (!nameDict.ContainsKey(fileName))
                    {
                        nameDict.Add(fileName, 0);
                    }
                    else
                    {
                        nameDict[fileName] += 1;
                        fileName = fileName.Replace(".", "_" + nameDict[fileName] + ".");
                    }
                    var image = new AssetImages()
                    {
                        AssetId = AssetId,
                        IsPrimary = item.IsPrimary,
                        AssetImage = imageToUpdate.Where(r => r.AssetImage == fileName).Count() > 0 ? Guid.NewGuid().ToString()+fileName : fileName
                    };

                    image.SetCreateInfo(currentUserId);

                    var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                        image, new string[] { "AssetImageId", "RecordGuid" }, "Assetimages");

                    var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");
                    AssetImages recordSaved = null;

                    if (result != null)
                    {
                        DynamicParameters dynamicParameters = new DynamicParameters();
                        dynamicParameters.Add("@Id", result.Id);
                        recordSaved = await _readRepository.GetLazyRepository<AssetImages>().Value
                        .GetAsync($@"select * from AssetImages Where AssetImageId = @Id", cancellationToken, dynamicParameters, dbTransaction, "text");
                    }

                    var sourceFileName = ExtractSourceFileName(item.AssetImage);
                    var sourcePath = await _azureBlobFileService.MapPath($"~/store/Temp/assetattachment/attachments/{sourceFileName}");
                    var destinationDir = await _azureBlobFileService.MapPath($"~/store/assetattachment/{command.AssetRegisterId}/{recordSaved?.RecordGuid}");
                    var destinationPath = $"{destinationDir}/{image.AssetImage}";
                    await _azureBlobFileService.MoveFileAsync(sourcePath, destinationPath);

                }

            }
            else
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@AssetId", AssetId);
                //dynamicParameters.Add("@pastAddedImages", pastAddedImages);
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                                @$" Delete From AssetImages Where AssetId = @AssetId",
                                cancellationToken, dynamicParameters, dbTransaction, "text");
            }
        }

        private static string ExtractSourceFileName(string attachmentName)
        {
            if (string.IsNullOrWhiteSpace(attachmentName))
                return string.Empty;

            var lastEqualIndex = attachmentName.LastIndexOf("=", StringComparison.Ordinal);
            return lastEqualIndex >= 0 && lastEqualIndex < attachmentName.Length - 1
                ? attachmentName.Substring(lastEqualIndex + 1)
                : attachmentName;
        }
    }
}
