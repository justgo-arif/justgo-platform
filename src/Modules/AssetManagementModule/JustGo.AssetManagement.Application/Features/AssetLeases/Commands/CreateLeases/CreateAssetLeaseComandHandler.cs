using System.Data;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetTypes.Queries.GetAssetMetadata;
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
using JustGo.AssetManagement.Application.Features.Workflows.Commands.WorkflowSubmissions;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using JustGo.AssetManagement.Application.Features.Common.Queries.CheckTranferPedingByAssetId;
using JustGo.Authentication.Infrastructure.Exceptions;
using System.Data.Common;
using JustGoAPI.Shared.Helper;
using AuditScheme = JustGo.Authentication.Infrastructure.Logging.AuditScheme;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases
{
    public class CreateAssetLeaseComandHandler : IRequestHandler<CreateAssetLeaseCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        LazyService<IReadRepository<dynamic>> _readDb;
        private readonly IAzureBlobFileService _azureBlobFileService;
        public CreateAssetLeaseComandHandler(
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
        public async Task<string> Handle(CreateAssetLeaseCommand request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var result = await SaveLease(currentUserId,request, cancellationToken);
            return result;
        }

        private async Task<string> SaveLease(int currentUserId, CreateAssetLeaseCommand command, CancellationToken cancellationToken)
        {
            var qTypeParameters = new DynamicParameters();
            qTypeParameters.Add("@RecordGuid", command.AssetTypeId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                  .GetAsync($@"Select * from AssetTypes Where RecordGuid = @RecordGuid",
                  cancellationToken, qTypeParameters, null, "text");

            var leaseConfig = JsonConvert.DeserializeObject<AssetLeaseConfig>(assetType.AssetLeaseConfig);

            var innerUsers = command.LeaseOwners.Where(r => !string.IsNullOrEmpty(r.LeaseOwnerId)).ToList();
            var outerrUsers = command.LeaseOwners.Where(r => string.IsNullOrEmpty(r.LeaseOwnerId)).ToList();

            var leaseUserIds = innerUsers.Any() ? await _mediator.Send(new GetIdByGuidQuery() { RecordGuids = innerUsers.Select(r => r.LeaseOwnerId??"").ToList(), Entity = AssetTables.User }, cancellationToken) : new List<int>();
            var req = _mapper.Map<AssetLease>(command);
            req.AssetId = command.AssetRegisterId != null ? (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { command.AssetRegisterId.ToString() } }))[0] : 0;
            req.StatusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status =  LeaseStatusType.PendingOwnerApproval }); 
            req.RecordGuid = Guid.NewGuid().ToString().ToUpper();

            if ((await _mediator.Send(new CheckTranferPedingByAssetIdQuery()
            {
                AssetRegisterId = command.AssetRegisterId.ToString()
            })))
            {
                throw new ConflictException("Already have a transfer in progress. Please complete it before proceeding.");
            }

            if (!leaseConfig.AllowedOverrideLeaseDate)
            {
                if(await LeaseExistsAsync(req.AssetId, req.LeaseStartDate, req.LeaseEndDate, cancellationToken))
                    return null;
            }

            //var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            req.SetCreateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateInsertSQLWithParameters(req,
            new string[] { "AssetLeaseId" },
                "AssetLeases");
            //var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");

            var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, null, "text");

            if (result != null)
            {

                foreach (var LeaseOwnerId in leaseUserIds)
                {
                    var ownerShip = new AssetOwnership()
                    {
                        AssetId = req.AssetId,
                        OwnerId = LeaseOwnerId,
                        OwnerType = OwnerType.Individual,
                        EntityId = result.Id,
                        EntityType = OwnershipEntityType.AssetLease

                    };

                    var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                        ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                    //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    // sql2, cancellationToken, qparams2, dbTransaction, "text");

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
                        AssetId = req.AssetId,
                        OwnerId = insertedUser.Id,
                        OwnerType = OwnerType.OuterIndividual,
                        EntityId = result.Id,
                        EntityType = OwnershipEntityType.AssetLease

                    };

                    var (sql3, qparams3) = SQLHelper.GenerateInsertSQLWithParameters(
                        ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                    //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    //  sql3, cancellationToken, qparams3, dbTransaction, "text");

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                            sql3, cancellationToken, qparams3, null, "text");
                }

               // await SaveAttachments(currentUserId, command, result.Id, req.RecordGuid, dbTransaction, cancellationToken);

                await SaveAttachments(currentUserId, command, result.Id, req.RecordGuid, null, cancellationToken);


            }


            var qOwnersParameters = new DynamicParameters();
            qOwnersParameters.Add("@AssetId", req.AssetId);

            var owners = await _readRepository.GetLazyRepository<AssetOwner>().Value
                  .GetListAsync($@"Select * from AssetOwners Where AssetId = @AssetId",
                  cancellationToken, qOwnersParameters, null, "text");


            int StepOrder = 1; //fixed order cause owner can submit approval randomly wihtout order.

            foreach (var owner in owners)
            {

                var step = new WorkflowStep()
                {
                        ResourceId = result.Id,
                        AssetTypeId = assetType.AssetTypeId,
                        StepName = "Owner Lease Approval",
                        WorkFlowType = WorkFlowType.OwnerLeaseApproval,
                        StepOrder  = StepOrder,
                        AuthorityType = AuthorityType.Individual,
                        AuthorityId = owner.OwnerId,
                        

                };

                var (sql4, qparams4) = SQLHelper.GenerateInsertSQLWithParameters(
                    step, new string[] { "StepId", "RecordGuid" }, "WorkflowSteps");

                //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                //  sql4, cancellationToken, qparams4, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sql4, cancellationToken, qparams4, null, "text");
            }

            //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {req.AssetId}, @LeaseId = {result.Id}, @AssetLicenseId = null",
            //                                    cancellationToken, null, dbTransaction, "text");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = {currentUserId}, @AssetId = {req.AssetId}, @LeaseId = {result.Id}, @AssetLicenseId = null",
                                    cancellationToken, null, null, "text");


           // await _unitOfWork.CommitAsync(dbTransaction);



            if (result != null)
            {

                CustomLog.Event(AuditScheme.AssetManagement.Value,
                    AuditScheme.AssetManagement.AssetLease.Value,
                    AuditScheme.AssetManagement.AssetLease.Created.Value,
                   currentUserId,
                   result.Id,
                   LogEntityType.Asset,
                   result.Id,
                   AuditScheme.AssetManagement.AssetLease.Created.Name,
                   "Asset Lease created;" + JsonConvert.SerializeObject(command)
                  );

                if (owners.Any(r => r.OwnerTypeId == OwnerType.Individual &&
                            r.OwnerId == currentUserId))
                {
                    var resultWorkflow = await _mediator.Send(new WorkflowSubmissionCommand()
                    {
                        WorkFlowType = WorkFlowType.OwnerLeaseApproval,
                        AssetTypeId = assetType.RecordGuid,
                        EntityId = req.RecordGuid,
                        ActionStatus = ActionStatus.Approve,
                        RejectionReason = RejectionReason.None,
                        Remarks = "",
                        ProxyUserId = null
                    });
                }

                await _mediator.Send(new SendAssetEmailCommand
                {
                    MessageScheme = "Asset/Owner approval for lease",
                    Argument = "",
                    ForEntityId = req.AssetId,
                    TypeEntityId = result.Id,
                    InvokeUserId = currentUserId,
                    OwnerId = 0,
                }, cancellationToken);

            }


            return req.RecordGuid;
        }

        private async Task SaveAttachments(int currentUserId, CreateAssetLeaseCommand command, int assetLeaseId, string assetLeaseGuid, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            Dictionary<string, int> nameDict = new Dictionary<string, int>();

            foreach (var item in command.LeaseAttachment)
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
        private async Task<bool> LeaseExistsAsync(int assetId, DateTime leaseStart, DateTime leaseEnd, CancellationToken cancellationToken)
        {
            string sql = @" SELECT TOP 1 1
                            FROM AssetLeases al
                            inner join AssetStatus lst on lst.AssetStatusId =  al.StatusId and lst.Type = 2
                            WHERE al.AssetId = @AssetId
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
                ExpiredStatus = Utilities.GetEnumText(LeaseStatusType.Expired)
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
