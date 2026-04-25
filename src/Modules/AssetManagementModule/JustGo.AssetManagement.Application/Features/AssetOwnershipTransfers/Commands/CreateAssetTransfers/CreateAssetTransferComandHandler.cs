using System.Data;
using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetTransferStatuses;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.AssetManagement.Application.Features.Workflows.Commands.WorkflowSubmissions;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using Newtonsoft.Json;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.AssetManagement.Application.Features.Common.Queries.CheckTranferPedingByAssetId;
using JustGo.AssetManagement.Application.Features.Common.Queries.CheckLeasePedingByAssetId;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using System.Data.Common;

namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Commands.CreateAssetTransfers
{
    public class CreateAssetTransferComandHandler : IRequestHandler<CreateAssetTransferCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IAzureBlobFileService _azureBlobFileService;
        public CreateAssetTransferComandHandler(
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
            _azureBlobFileService = azureBlobFileService;
        }
        public async Task<string> Handle(CreateAssetTransferCommand request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var result = await SaveTransfer(currentUserId,request, cancellationToken);
            return result;
        }

        private async Task<string> SaveTransfer(int currentUserId, CreateAssetTransferCommand command, CancellationToken cancellationToken)
        {
            var qTypeParameters = new DynamicParameters();
            qTypeParameters.Add("@RecordGuid", command.AssetRegisterId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                  .GetAsync($@"declare @AssetTypeId int = 
                                       (Select AssetTypeId from AssetRegisters Where RecordGuid = @RecordGuid);
                               Select * from AssetTypes Where AssetTypeId = @AssetTypeId",
                  cancellationToken, qTypeParameters, null, "text");


            var transferConfig = JsonConvert.DeserializeObject<AssetTransferConfig>(assetType.AssetTransferConfig);

            var transferUserIds = command.TransferOwners.Any() ? await _mediator.Send(new GetIdByGuidQuery() { RecordGuids = command.TransferOwners.Select(r => r.TransferOwnerId??"").ToList(), Entity = AssetTables.User }, cancellationToken) : new List<int>();
            var req = _mapper.Map<AssetOwnershipTransfer>(command);
            req.TransferDate = DateTime.UtcNow;
            req.AssetId = command.AssetRegisterId != null ? (await _mediator.Send(new GetIdByGuidQuery() { Entity = AssetTables.AssetRegisters, RecordGuids = new List<string>() { command.AssetRegisterId } }))[0] : 0;
            req.RecordGuid = Guid.NewGuid().ToString().ToUpper();


            var qOwnersParameters = new DynamicParameters();
            qOwnersParameters.Add("@AssetId", req.AssetId);

            var owners = await _readRepository.GetLazyRepository<AssetOwner>().Value
                  .GetListAsync($@"Select * from AssetOwners Where AssetId = @AssetId",
                  cancellationToken, qOwnersParameters, null, "text");

            if (owners.Any())
            {
                req.TransferStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.PendingOwnerApproval });
            }
            else if (transferConfig.AllowedPayment)
            {
                req.TransferStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.PendingPayment });                      
            }
            else
            {
                req.TransferStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.PendingConfirmation });
            }
                

            if(await _mediator.Send(
                new CheckTranferPedingByAssetIdQuery(){
                  AssetRegisterId = command.AssetRegisterId.ToString()
                }))
            { 
                throw new  ConflictException("Already have a transfer in progress. Please complete it before proceeding.");
            }


            if (await _mediator.Send(
                new CheckLeasePedingByAssetIdQuery()
                {
                    AssetRegisterId = command.AssetRegisterId.ToString()
                }))
            {
                throw new ConflictException("Already have a lease in progress. Please complete it before proceeding.");
            }

            //var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            req.SetCreateInfo(currentUserId);
            var (sql, qparams) = SQLHelper
                .GenerateInsertSQLWithParameters(req,
            new string[] { "AssetOwnershipTransferId" },
                "AssetOwnershipTransfers");

            //var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");
            var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, null, "text");


            //CustomLog.Event(AuditScheme.AssetManagement.Value,
            //    AuditScheme.AssetManagement.AssetTransfer.Value,
            //    AuditScheme.AssetManagement.AssetTransfer.Created.Value,
            //   currentUserId,
            //   result.Id,
            //   LogEntityType.Asset,
            //   result.Id,
            //   AuditScheme.AssetManagement.AssetTransfer.Created.Name,
            //   "Asset Transfer inserted;" + JsonConvert.SerializeObject(result)
            //  );

            if (result != null)
            {

                foreach (var TransferOwnerId in transferUserIds)
                {
                    var owner = new AssetTransferOwner()
                    {
                        AssetOwnershipTransferId = result.Id,
                        OwnerId = TransferOwnerId,
                        OwnerType = OwnerType.Individual,

                    };

                    owner.SetCreateInfo(currentUserId);
                    var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                        owner, new string[] { "AssetTransferOwnerId", "RecordGuid" }, "AssetTransferOwners");

                    //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    //    sql2, cancellationToken, qparams2, dbTransaction, "text");

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        sql2, cancellationToken, qparams2, null, "text");

                    var ownerShip = new AssetOwnership()
                    {
                        AssetId = req.AssetId,
                        OwnerId = TransferOwnerId,
                        OwnerType = OwnerType.Individual,
                        EntityId = result.Id,
                        EntityType = OwnershipEntityType.AssetTransfer

                    };

                    var (sql3, qparams3) = SQLHelper.GenerateInsertSQLWithParameters(
                        ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                    // await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    //     sql3, cancellationToken, qparams3, dbTransaction, "text");

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                           sql3, cancellationToken, qparams3, null, "text");

                }

                //await SaveAttachments(currentUserId, command, result.Id, req.RecordGuid, dbTransaction, cancellationToken);
                await SaveAttachments(currentUserId, command, result.Id, req.RecordGuid, null, cancellationToken);

            }




            int StepOrder = 1; //fixed order cause owner can submit approval randomly wihtout order.

            foreach (var owner in owners)
            {

                var step = new WorkflowStep()
                {
                        ResourceId = result.Id,
                        AssetTypeId = assetType.AssetTypeId,
                        StepName = "Owner Transfer Approval",
                        WorkFlowType = WorkFlowType.OwnerTransferApproval,
                        StepOrder  = StepOrder,
                        AuthorityType = AuthorityType.Individual,
                        AuthorityId = owner.OwnerId,
                        

                };

                var (sql4, qparams4) = SQLHelper.GenerateInsertSQLWithParameters(
                    step, new string[] { "StepId", "RecordGuid" }, "WorkflowSteps");

                //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                //    sql4, cancellationToken, qparams4, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sql4, cancellationToken, qparams4, null, "text");
            }



            var dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@currentUserId", currentUserId);
            dynamicParameters.Add("@TransferAssetId", req.AssetId);
            //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @TransferAssetId, @LeaseId = null, @AssetLicenseId = null",
            //                                        cancellationToken, dynamicParameters, dbTransaction, "text");

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @TransferAssetId, @LeaseId = null, @AssetLicenseId = null",
                                                    cancellationToken, dynamicParameters, null, "text");




            //await _unitOfWork.CommitAsync(dbTransaction);



            if (result != null)
            {

                CustomLog.Event(AuditScheme.AssetManagement.Value,
                    AuditScheme.AssetManagement.AssetTransfer.Value,
                    AuditScheme.AssetManagement.AssetTransfer.Created.Value,
                   currentUserId,
                   result.Id,
                   LogEntityType.Asset,
                   result.Id,
                   AuditScheme.AssetManagement.AssetTransfer.Created.Name,
                   "Asset Transfer created;" + JsonConvert.SerializeObject(command)
                  );

                if (owners.Any(r => r.OwnerTypeId == OwnerType.Individual &&
                            r.OwnerId == currentUserId))
                {


                    var qTransferParameters = new DynamicParameters();
                    qTransferParameters.Add("@Id", result.Id);


                    //CustomLog.Event(AuditScheme.AssetManagement.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Created.Value,
                    //   currentUserId,
                    //   result.Id,
                    //   LogEntityType.Asset,
                    //   assetType.AssetTypeId,
                    //   AuditScheme.AssetManagement.AssetTransfer.Created.Name,
                    //   "qTransferParameters;" + JsonConvert.SerializeObject(
                    //       result.Id)
                    //  );

                    var assetOwnershipTransfer = await _readRepository.GetLazyRepository<AssetOwnershipTransfer>().Value
                          .GetAsync($@"select * from AssetOwnershipTransfers where AssetOwnershipTransferId = @Id",
                          cancellationToken, qTransferParameters, null, "text");

                    //CustomLog.Event(AuditScheme.AssetManagement.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Created.Value,
                    //   currentUserId,
                    //   result.Id,
                    //   LogEntityType.Asset,
                    //   assetType.AssetTypeId,
                    //   AuditScheme.AssetManagement.AssetTransfer.Created.Name,
                    //   $"transfer data {assetType.RecordGuid};" + JsonConvert.SerializeObject(
                    //       assetOwnershipTransfer)
                    //  );

                    var obj = new WorkflowSubmissionCommand()
                    {
                        WorkFlowType = WorkFlowType.OwnerTransferApproval,
                        AssetTypeId = assetType.RecordGuid,
                        EntityId = assetOwnershipTransfer.RecordGuid,
                        ActionStatus = ActionStatus.Approve,
                        RejectionReason = RejectionReason.None,
                        Remarks = "",
                        ProxyUserId = null
                    };

                    //CustomLog.Event(AuditScheme.AssetManagement.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Value,
                    //    AuditScheme.AssetManagement.AssetTransfer.Created.Value,
                    //   currentUserId,
                    //   result.Id,
                    //   LogEntityType.Asset,
                    //   result.Id,
                    //   AuditScheme.AssetManagement.AssetTransfer.Created.Name,
                    //   "obj data;" + JsonConvert.SerializeObject(new
                    //   {
                    //       obj
                    //   })
                    //  );

                    var resultWorkflow = await _mediator.Send(obj);
                }

                await _mediator.Send(new SendAssetEmailCommand
                {
                    MessageScheme = "Asset/Current Owner Change Approval Notification",
                    Argument = "",
                    ForEntityId = req.AssetId,
                    TypeEntityId = result.Id,
                    InvokeUserId = currentUserId,
                    OwnerId = 0,
                }, cancellationToken);

            }


            return req.RecordGuid;
        }

        private async Task SaveAttachments(int currentUserId, CreateAssetTransferCommand command, int assetTransferId, string assetTransferGuid, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            Dictionary<string, int> nameDict = new Dictionary<string, int>();

            foreach (var item in command.TransferAttachment)
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

                var transferAttachment = new AssetTransferAttachment()
                {
                    AssetOwnershipTransferId = assetTransferId,
                    AttachmentPath = fileName,
                    RecordGuid = Guid.NewGuid().ToString().ToUpper(),
                };

                var (sql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                    transferAttachment, new string[] { "TransferAttachmentId" }, "AssetTransferAttachments");

                var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");
                               
                var sourceFileName = ExtractSourceFileName(item.AttachmentName);
                var sourcePath = await _azureBlobFileService.MapPath($"~/store/Temp/assetattachment/attachments/{sourceFileName}");
                var destinationDir = await _azureBlobFileService.MapPath($"~/store/assetattachment/{assetTransferGuid}/{transferAttachment.RecordGuid}");
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
