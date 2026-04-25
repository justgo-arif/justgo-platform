using System.Data;
using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Serilog;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using Newtonsoft.Json;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using StackExchange.Redis;
using System.Data.Common;
using System.Transactions;
using JustGoAPI.Shared.Helper;
using AuditScheme = JustGo.Authentication.Infrastructure.Logging.AuditScheme;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases
{
    public class EditAssetLeaseCommandComandHandler : IRequestHandler<EditAssetLeaseCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        LazyService<IReadRepository<dynamic>> _readDb;
        private readonly IAzureBlobFileService _azureBlobFileService;
        public EditAssetLeaseCommandComandHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IUtilityService utilityService,
            LazyService<IReadRepository<dynamic>> readDb
            , IAzureBlobFileService azureBlobFileService
            )
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _readDb = readDb;
            _azureBlobFileService = azureBlobFileService;
        }
        public async Task<string> Handle(EditAssetLeaseCommand request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var result = await SaveLease(currentUserId,request, cancellationToken);
            return result;
        }

        private DateTime GetCurrentDate()
        {
            return DateTime.Today;
        }

        private async Task<string> SaveLease(int currentUserId, EditAssetLeaseCommand command, CancellationToken cancellationToken)
        {
            var leaseQParameters = new DynamicParameters();
            leaseQParameters.Add("@RecordGuid", command.AssetLeaseId);
            var lease = await _readRepository.GetLazyRepository<AssetLease>().Value
             .GetAsync($@"Select * from AssetLeases Where RecordGuid = @RecordGuid",
               cancellationToken, leaseQParameters, null, "text");

            var qTypeParameters = new DynamicParameters();
            qTypeParameters.Add("@AssetId", lease.AssetId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                  .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                             Select AssetTypeId from AssetRegisters Where AssetId = @AssetId)",
                  cancellationToken, qTypeParameters, null, "text");

            var leaseConfig = JsonConvert.DeserializeObject<AssetLeaseConfig>(assetType.AssetLeaseConfig);

            var innerUsers = command.LeaseOwners.Where(r => !string.IsNullOrEmpty(r.LeaseOwnerId)).ToList();
            var outerrUsers = command.LeaseOwners.Where(r => string.IsNullOrEmpty(r.LeaseOwnerId)).ToList();

            var leaseUserIds = innerUsers.Any() ? await _mediator.Send(new GetIdByGuidQuery() { RecordGuids = innerUsers.Select(r => r.LeaseOwnerId??"").ToList(), Entity = AssetTables.User }, cancellationToken) : new List<int>();

            lease.LeaseStartDate = command.LeaseStartDate;
            lease.LeaseEndDate = command.LeaseEndDate;
            lease.DateRangeType = command.DateRangeType;   
            lease.Comments = command.Comments;

            int activeStatusId = await _mediator.Send(new GetLeaseStatusIdQuery()
            {
                Status = LeaseStatusType.Active
            });

            int expiredStatusId = await _mediator.Send(new GetLeaseStatusIdQuery()
            {
                Status = LeaseStatusType.Expired
            });

            if (command.LeaseStartDate > GetCurrentDate() 
                && 
                (lease.StatusId == activeStatusId ||
                 lease.StatusId == expiredStatusId))
            {
                lease.StatusId = await _mediator.Send(new GetLeaseStatusIdQuery()
                {
                    Status = LeaseStatusType.Scheduled
                });
            }
            else if (command.LeaseEndDate > GetCurrentDate()
                      &&
                     lease.StatusId == expiredStatusId)
            {
                lease.StatusId = await _mediator.Send(new GetLeaseStatusIdQuery()
                {
                    Status = LeaseStatusType.Active
                });
            }

            if (!leaseConfig.AllowedOverrideLeaseDate)
            {
                if (await LeaseExistsAsync(lease.AssetId, command.AssetLeaseId, lease.LeaseStartDate, lease.LeaseEndDate, cancellationToken))
                    return null;
            }

            //var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            lease.SetUpdateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateUpdateSQLWithParameters(
                lease,
                "AssetLeaseId",
                new string[] { },
                "AssetLeases");
            //var result = await _writeRepository.GetLazyRepository<object>().Value
              //  .ExecuteAsync(sql, cancellationToken, qparams, dbTransaction, "text");

            var result = await _writeRepository.GetLazyRepository<object>().Value
               .ExecuteAsync(sql, cancellationToken, qparams, null, "text");

            /*await _writeRepository.GetLazyRepository<object>().Value
            .ExecuteAsync($@"delete from OuterUsers 
                             where OuterUserId in (
                                select OwnerId from AssetOwnerships 
                                where EntityId = {lease.AssetLeaseId} and 
                                      EntityType = 2 and
                                      OwnerType = 3
                             )",
            cancellationToken, null, dbTransaction, "text");*/

            await _writeRepository.GetLazyRepository<object>().Value
            .ExecuteAsync($@"delete from OuterUsers 
                                         where OuterUserId in (
                                            select OwnerId from AssetOwnerships 
                                            where EntityId = {lease.AssetLeaseId} and 
                                                  EntityType = 2 and
                                                  OwnerType = 3
                                         )
                        ",
            cancellationToken, null, null, "text");

            /*await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync($@"delete from AssetOwnerships 
                                where EntityId = {lease.AssetLeaseId} and 
                                      EntityType = 2 ",
                cancellationToken, null, dbTransaction, "text");*/

            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync($@"delete from AssetOwnerships 
                                            where EntityId = {lease.AssetLeaseId} and 
                                                  EntityType = 2 ",
                cancellationToken, null, null, "text");

            foreach (var LeaseOwnerId in leaseUserIds)
            {
                var ownerShip = new AssetOwnership()
                {
                    AssetId = lease.AssetId,
                    OwnerId = LeaseOwnerId,
                    OwnerType = OwnerType.Individual,
                    EntityId = lease.AssetLeaseId,
                    EntityType = OwnershipEntityType.AssetLease

                };

                var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                    ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                //sql2, cancellationToken, qparams2, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sql2, cancellationToken, qparams2, null, "text");
            }

            foreach (var owner in outerrUsers)
            {
                var outerUser = new OuterUser()
                {
                    Email = owner.Email,
                    FirstName = owner.FirstName,
                    LastName = owner.LastName

                };

                outerUser.SetCreateInfo(currentUserId);

                var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                    outerUser, new string[] { "OuterUserId", "RecordGuid" }, "OuterUsers");

                //var insertedUser = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(
                //sql2, cancellationToken, qparams2, dbTransaction, "text");


                var insertedUser = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(
                    sql2, cancellationToken, qparams2, null, "text");

                var ownerShip = new AssetOwnership()
                {
                    AssetId = lease.AssetId,
                    OwnerId = insertedUser.Id,
                    OwnerType = OwnerType.OuterIndividual,
                    EntityId = lease.AssetLeaseId,
                    EntityType = OwnershipEntityType.AssetLease

                };

                var (sql3, qparams3) = SQLHelper.GenerateInsertSQLWithParameters(
                    ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                // await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                //     sql3, cancellationToken, qparams3, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sql3, cancellationToken, qparams3, null, "text");
            }

            //await SaveAttachments(command, lease.AssetLeaseId, lease.RecordGuid, dbTransaction, cancellationToken);

            await SaveAttachments(command, lease.AssetLeaseId, lease.RecordGuid, null, cancellationToken);


            //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {lease.AssetId}, @LeaseId = {lease.AssetLeaseId}, @AssetLicenseId = null",
            //                                               cancellationToken, null, dbTransaction, "text");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {lease.AssetId}, @LeaseId = {lease.AssetLeaseId}, @AssetLicenseId = null",
                                                cancellationToken, null, null, "text");


            //await _unitOfWork.CommitAsync(dbTransaction);

            if (result != null)
            {

                Log.Logger
                .ForContext("DocId", lease.AssetLeaseId)
                .ForContext("ActionUserId", currentUserId)
                .ForContext("Category", AuditScheme.AssetManagement.Value)
                .ForContext("SubCategory", AuditScheme.AssetManagement.AssetLease.Value)
                .ForContext("Action", AuditScheme.AssetManagement.AssetLease.Updated.Value)
                .ForContext("AffectedEntityType", LogEntityType.Asset)
                .ForContext("ActionType", AuditScheme.AssetManagement.AssetLease.Updated.Name)
                .ForContext("OwningEntitydType", LogEntityType.Asset)
                .ForContext("Details", JsonConvert.SerializeObject(command))
                .ForContext("ActionName", "EditAssetLeaseCommand")
                .ForContext("OwningEntityId", lease.AssetLeaseId)
                .Information("Asset Lease created: {@AssetLease}",
                    command
                );

            }


            return lease.RecordGuid;
        }

        private async Task SaveAttachments(EditAssetLeaseCommand command, int assetLeaseId, string assetLeaseGuid, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            Dictionary<string, int> nameDict = new Dictionary<string, int>();

            var oldFileIds = command.LeaseAttachment.Where(r =>
                                       r.LeaseAttachmentId != null &&
                                       r.LeaseAttachmentId != "").
                                       Select(r => r.LeaseAttachmentId).ToList();

            var oldFileDeleteParams = new DynamicParameters();
            oldFileDeleteParams.Add("@AssetLeaseId", assetLeaseId);
            oldFileDeleteParams.Add("@RecordGuids", oldFileIds);
            await _writeRepository.GetLazyRepository<object>().Value
                .ExecuteAsync($@"delete from AssetLeaseAttachments 
                                where  AssetLeaseId = @AssetLeaseId and
                                       RecordGuid not in @RecordGuids",
                cancellationToken, oldFileDeleteParams, dbTransaction, "text");

            var newFiles = command.LeaseAttachment.Where(r =>
                                       r.LeaseAttachmentId == null ||
                                       r.LeaseAttachmentId == "");

            foreach (var item in newFiles)
            {
                var rssparams = new DynamicParameters();

                string fileName = item.AttachmentName.Split(new string[] { "_$_" }, StringSplitOptions.None).Last();
                if (!nameDict.ContainsKey(fileName))
                {
                    nameDict.Add(fileName, 0);
                }
                else
                {
                    nameDict[fileName] += 1;
                    fileName = fileName.Replace(".", "_" + nameDict[fileName] + ".");
                }

                var leaseAttachment = new AssetLeaseAttachment()
                {
                    AssetLeaseId = assetLeaseId,
                    AttachmentPath = fileName,
                    RecordGuid = Guid.NewGuid().ToString().ToUpper(),
                };

                var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                    leaseAttachment, new string[] { "LeaseAttachmentId" }, "AssetLeaseAttachments");

                var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");
                               
                var sourceFileName = ExtractSourceFileName(item.AttachmentName);
                var sourcePath = await _azureBlobFileService.MapPath($"~/store/Temp/assetattachment/attachments/{sourceFileName}");
                var destinationDir = await _azureBlobFileService.MapPath($"~/store/assetleaseattachment/{assetLeaseGuid}/{leaseAttachment.RecordGuid}");
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
        private async Task<bool> LeaseExistsAsync(int assetId, string AssetLeaseId, DateTime leaseStart, DateTime leaseEnd, CancellationToken cancellationToken)
        {
            string sql = @"SELECT TOP 1 1
                            FROM AssetLeases al
                            inner join AssetStatus lst on lst.AssetStatusId =  al.StatusId and lst.Type = 2
                            WHERE al.AssetId = @AssetId And al.RecordGuid != @AssetLeaseId
                            AND lst.Name NOT IN (@RejectedStatus, @CancelledStatus, @ExpiredStatus)
                            AND ((@LeaseStart BETWEEN al.LeaseStartDate AND al.LeaseEndDate)
                                 OR
                                 (@LeaseEnd BETWEEN al.LeaseStartDate AND al.LeaseEndDate)
                                 OR
                                 (al.LeaseStartDate >= @LeaseStart AND al.LeaseEndDate <= @LeaseEnd ))
                            ";

            var parameters = new
            {
                AssetId = assetId,
                LeaseStart = leaseStart,
                LeaseEnd = leaseEnd,
                RejectedStatus = Utilities.GetEnumText(LeaseStatusType.Rejected),
                CancelledStatus = Utilities.GetEnumText(LeaseStatusType.Cancelled),
                ExpiredStatus = Utilities.GetEnumText(LeaseStatusType.Expired),
                AssetLeaseId = AssetLeaseId
            };

            var result = await _readDb.Value.GetSingleAsync(sql, cancellationToken, parameters, null, "text");
            if( result == null)
            {
                return false;
            }
            return true; 
        }

    }
}
