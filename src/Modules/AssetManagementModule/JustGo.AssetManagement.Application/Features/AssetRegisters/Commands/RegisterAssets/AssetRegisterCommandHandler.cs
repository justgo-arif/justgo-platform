using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetValidationHelpers;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetAssetStatuses;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using Serilog;
using Newtonsoft.Json;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.RegisterAssets
{
    public class AssetRegisterCommandHandler : IRequestHandler<AssetRegisterCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LazyService<IReadRepository<dynamic>> _readDb;
        private readonly IUtilityService _utilityService;
        private readonly IAzureBlobFileService _azureBlobFileService;
        private readonly ICustomError _error;
        public AssetRegisterCommandHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            LazyService<IReadRepository<dynamic>> readdb,
            IUtilityService utilityService,
            IAzureBlobFileService azureBlobFileService
            , ICustomError error)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _readDb = readdb;
            _utilityService = utilityService;
            _azureBlobFileService = azureBlobFileService;
            _error = error;
        }

        public async Task<string> Handle(AssetRegisterCommand command, CancellationToken cancellationToken)
        {


            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            return await SaveAsset(currentUserId, command, cancellationToken);

        }


        private async Task<string> SaveAsset(int currentUserId, AssetRegisterCommand command, CancellationToken cancellationToken)
        {

            var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var req = _mapper.Map<AssetRegister>(command);

            string RecordGuid = "";

            req.AssetCategoryId = !string.IsNullOrWhiteSpace(command.CategoryId) ?  (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetCategories, RecordGuids = new List<string>() { command.CategoryId } }))[0] : null;
            req.AssetTypeId = (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetTypes, RecordGuids = new List<string>() { command.TypeId } }))[0];
            req.IssueDate = DateTime.UtcNow;
            req.AssetReference = "";

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@AssetTypeId", req.AssetTypeId);
            dynamicParameters.Add("@currentUserId", currentUserId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
             .GetAsync($@"Select * from AssetTypes Where AssetTypeId = @AssetTypeId", cancellationToken, dynamicParameters, dbTransaction, "text");


            var userGroups = await _readRepository.GetLazyRepository<MapItemDTO<string, string>>().Value
             .GetListAsync($@"select 
                          g.[Name] [Key],
                          g.[Name] Value
                        from groupmembers gm 

                        inner join [Group] g on 
                        gm.GroupId= g.GroupId where 
                        gm.UserId = @currentUserId", cancellationToken, dynamicParameters, dbTransaction, "text");

            var (isValid, message) = await AssetValidator.Validate(_mediator, command, assetType, null, userGroups.ToList());

            if (!isValid)
            {
                if(message == null)
                {
                    throw new CustomValidationException("Asset Validation Failed!");
                }
                else
                {
                    throw new CustomValidationException(message);
                }
                    
            }

            AssetRegister recordSaved = null;

            req.StatusId = await _mediator.Send(new GetAssetStatusIdQuery() { Status = AssetStatusType.Draft});
            req.SetCreateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateInsertSQLWithParameters(req,
                new string[] { "AssetId", "RecordGuid" },
                "AssetRegisters");
            var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");

            dynamicParameters = new DynamicParameters();
          

            if (result != null)
            {
                dynamicParameters.Add("@AssetId", result.Id);
                recordSaved = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                    .GetAsync($@"select * from AssetRegisters Where AssetId = @AssetId", cancellationToken, dynamicParameters, dbTransaction, "text");
                RecordGuid = recordSaved.RecordGuid;

            }


            if (recordSaved != null)
            {
                

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    $@"exec [SetNewAutoId] @Type = 1, @ResourceId = @AssetId", 
                    cancellationToken, dynamicParameters, dbTransaction, "text");
                await SaveTags(currentUserId, command, recordSaved.AssetId, recordSaved.AssetTypeId, dbTransaction, cancellationToken);
                await SaveOwners(currentUserId, command, recordSaved.AssetId, dbTransaction, cancellationToken);
                await SaveImages(currentUserId, command, recordSaved.AssetId, RecordGuid, dbTransaction, cancellationToken);

            }

            await _unitOfWork.CommitAsync(dbTransaction);

            CustomLog.Event(AuditScheme.AssetManagement.Value,
            AuditScheme.AssetManagement.General.Value,
            AuditScheme.AssetManagement.General.Created.Value,
            currentUserId,
            recordSaved.AssetId,
            LogEntityType.Asset,
            recordSaved.AssetId,
            AuditScheme.AssetManagement.General.Created.Name,
            "Asset Created;" + JsonConvert.SerializeObject(new
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


            return RecordGuid;
        }

        private async Task SaveTags(int currentUserId, AssetRegisterCommand command, int AssetId, int AssetTypeId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {




            if (command.AssetTags.Any())
            {

                string tagSqlConditions = string.Join(" or ", command.AssetTags.Select((t, i) => $@" Name = @Name{i} "));

                var tagSql = "select * from AssetTypesTag where AssetTypeId = @AssetTypeId and ( " +
                         tagSqlConditions + " ) ";

                var queryParameters = new DynamicParameters();

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

        private async Task SaveOwners(int currentUserId, AssetRegisterCommand command, int AssetId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {



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

                var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().
                    Value.ExecuteMultipleAsync(
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
        }

        private async Task SaveImages(int currentUserId, AssetRegisterCommand command, int AssetId, string assetGuid, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            Dictionary<string, int> nameDict = new Dictionary<string, int>();

            foreach (var item in command.AssetImages)
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
                    AssetImage = fileName
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
                var destinationDir = await _azureBlobFileService.MapPath($"~/store/assetattachment/{assetGuid}/{recordSaved?.RecordGuid}");
                var destinationPath = $"{destinationDir}/{fileName}";
                await _azureBlobFileService.MoveFileAsync(sourcePath, destinationPath);

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
