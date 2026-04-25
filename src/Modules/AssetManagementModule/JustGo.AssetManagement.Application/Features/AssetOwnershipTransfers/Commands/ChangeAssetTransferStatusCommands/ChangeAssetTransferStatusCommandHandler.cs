using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetTransferStatuses;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using AuthModule.Application.DTOs.Lookup;
using Serilog;
using JustGo.Authentication.Infrastructure.Logging;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses;
using static Dapper.SqlMapper;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetTransferStatusCommands
{
    public class ChangeAssetTransferStatusCommandHandler : IRequestHandler<ChangeAssetTransferStatusCommand, string>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeAssetTransferStatusCommandHandler(
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

        public async Task<string> Handle(ChangeAssetTransferStatusCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            return await ChangeStatus(command, currentUserId, cancellationToken);
        }


        private async Task<bool> HierarkeyCheck(List<int> checkingUserIds, int currentUserId, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@currentUserId", currentUserId);
            queryParameters.Add("@checkingUserIds", checkingUserIds);

            var adminRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                              .Value.GetListAsync($@"select [Name] [Text], Id [Value] from AbacRoles 
                                                    where Name in(
                                                    'System Admin',
                                                    'Asset Super Admin',
                                                    'Asset Admin',
                                                    'Asset Manager'
                                                    )", cancellationToken, null, null, "text")).ToList();


            queryParameters.Add("@adminRoles", adminRoles.Select(r => r.Value).ToList());

            var dataSql = $@"WITH
                        UserHierarchyLinks as (
                            select h.[HierarchyId] [HierarchyId] from 
                            HierarchyLinks hl
                            Inner join Hierarchies h  on  h.[Id] = hl.[HierarchyId]
	                        Left join AbacUserRoles abcr  on  abcr.UserId = hl.UserId and abcr.OrganizationId = h.EntityId
							where hl.[UserId] = @CurrentUserId and 
                            (abcr.RoleId in @adminRoles or
                             Exists(
							    select * from GroupMembers 
							    where GroupId in ( 25,1)
							    And UserId = @CurrentUserId
							 )
                            )
                        ),
                        UserClubs as (

                            select h.EntityId ClubId from 
                            UserHierarchyLinks hl
                            Inner join Hierarchies h  on  h.[HierarchyId].IsDescendantOf(hl.[HierarchyId]) = 1
                        )
                        Select Count(*) TotalRowCount from UserClubs uc
                        Inner join ClubMemberRoles cmr  on  cmr.ClubDocId = uc.ClubId where  cmr.UserId in @checkingUserIds";

            var count = (int)await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(dataSql, cancellationToken, queryParameters, null, "text");

            return count > 0;
        }


        private async Task<string> ChangeStatus(ChangeAssetTransferStatusCommand command, int currentUserId, CancellationToken cancellationToken)
        {



            int statusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = command.Status });
            int pendingConfirmationStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.PendingConfirmation });
            int pendingPaymentStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.PendingPayment });
            int activeStatusId = await _mediator.Send(new GetTransferStatusIdQuery() { Status = TransferStatusType.Completed });

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetTransferId);
            queryParameters.Add("@CurrentUserId", currentUserId);

            var assetOwnerCheck = await _readRepository.GetLazyRepository<AssetRegister>().Value
                           .GetAsync($@"Select * from AssetRegisters ar where 
                            ar.AssetId = (
                                Select Top(1) ao.AssetId from AssetOwners ao where ao.OwnerTypeId = 2 and ao.OwnerId = @CurrentUserId
                                And ao.AssetId = (
                                    Select atr.AssetId from AssetOwnershipTransfers atr where atr.RecordGuid = @RecordGuid
                                )
                            )                            
                           ", cancellationToken, queryParameters, null, "text");

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                            select AssetTypeId from AssetRegisters where AssetId = (
                                Select AssetId from AssetOwnershipTransfers atr where RecordGuid = @RecordGuid
                            )
                        )", cancellationToken, queryParameters, null, "text");



            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);


            var assetTransfer = await _readRepository.GetLazyRepository<AssetOwnershipTransfer>().Value
                    .GetAsync($@"select * from AssetOwnershipTransfers Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            var TransferToUsersQParams = new DynamicParameters();
            TransferToUsersQParams.Add("@AssetTransferId", assetTransfer.AssetOwnershipTransferId);

            var TransferToUsers = (await _readRepository.GetLazyRepository<AssetOwnership>().Value
                        .GetListAsync($@"select * from AssetTransferOwners Where AssetOwnershipTransferId = @AssetTransferId and OwnerType = 2", cancellationToken, TransferToUsersQParams, null, "text")).ToList();

            var qOwnersParameters = new DynamicParameters();
            qOwnersParameters.Add("@AssetTransferId", assetTransfer.AssetOwnershipTransferId);
            qOwnersParameters.Add("@currentUserId", currentUserId);

            var isTransferToFamily = await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                          .GetAsync(@"Select Top 1 u.FirstName [Text], u.UserId [Value]
                            FROM [User] u
						    inner join Family_Links fm on fm.Entityid = u.MemberDocId
                            inner join AssetTransferOwners ao on OwnerId = u.UserId and OwnerType = 2
                            Where   ao.AssetOwnershipTransferId = @AssetTransferId and
                                    fm.Docid in (Select DocId  
                                    from  Family_Links fl where fl.Entityid = (
                                    Select MemberDocId from [User]
                                    where userid = @currentUserId))",
          cancellationToken, qOwnersParameters, null, "text");

            if ((TransferToUsers.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) ||
                 isTransferToFamily != null ||
                 (await HierarkeyCheck(TransferToUsers.Select(r => r.OwnerId ?? 0).ToList(), currentUserId, cancellationToken))
                ) &&
                ((command.Status == TransferStatusType.PendingApproval && assetTransfer.TransferStatusId == pendingConfirmationStatusId) ||
                 (command.Status == TransferStatusType.Rejected && 
                  (assetTransfer.TransferStatusId == pendingConfirmationStatusId || assetTransfer.TransferStatusId == pendingPaymentStatusId)
                 )
                )
               )
            {


                if (command.Status == TransferStatusType.PendingApproval)
                {
                    queryParameters = new DynamicParameters();
                    queryParameters.Add("@AssetTypeId", assetType.AssetTypeId);
                    var workflowStep = await _readRepository.GetLazyRepository<WorkflowStep>().Value
                        .GetAsync($@"Select Top(1) * from WorkflowSteps Where AssetTypeId = @AssetTypeId and WorkFlowType = 3", cancellationToken, queryParameters, null, "text");

                    if (workflowStep == null)
                    {
                        statusId = activeStatusId;
                    }

                }
                else if (command.Status == TransferStatusType.Rejected)
                {
                    assetTransfer.RejectionReason = command.RejectionReason ?? RejectionReason.None;
                    assetTransfer.RecordRemarks = command.RejectionNote;

                }


                var dbTransaction = await _unitOfWork.BeginTransactionAsync();

                assetTransfer.SetUpdateInfo(currentUserId);
                assetTransfer.TransferStatusId = statusId;

                var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(assetTransfer, "RecordGuid",
                    new string[] { "AssetOwnershipTransferId", "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                    "AssetOwnershipTransfers");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, dbTransaction, "text");

                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@TransferAssetId", assetTransfer.AssetId);
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @TransferAssetId, @LeaseId = null, @AssetLicenseId = null",
                                                        cancellationToken, dynamicParameters, dbTransaction, "text");

                await _unitOfWork.CommitAsync(dbTransaction);

                int action = 0;
                string actionName = "";

                if (command.Status == TransferStatusType.Rejected)
                {
                    action = AuditScheme.AssetManagement.AssetTransfer.Rejected.Value;
                    actionName = AuditScheme.AssetManagement.AssetTransfer.Rejected.Name;
                }
                else if (command.Status == TransferStatusType.PendingApproval)
                {
                    action = AuditScheme.AssetManagement.AssetTransfer.ChangeStatus.Value;
                    actionName = AuditScheme.AssetManagement.AssetTransfer.ChangeStatus.Name;
                }


                CustomLog.Event(AuditScheme.AssetManagement.Value,
                   AuditScheme.AssetManagement.AssetTransfer.Value,
                   action,
                   currentUserId,
                   assetTransfer.AssetOwnershipTransferId,
                   LogEntityType.Asset,
                   assetTransfer.AssetOwnershipTransferId,
                   actionName,
                   "Asset Transfer status changed;" + JsonConvert.SerializeObject(assetTransfer)
                  );


                if(command.Status == TransferStatusType.Rejected)
                {
                    if(TransferToUsers.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) ||
                       isTransferToFamily != null)
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/New Owner On Reject",
                            Argument = "",
                            ForEntityId = assetTransfer.AssetId,
                            TypeEntityId = assetTransfer.AssetOwnershipTransferId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Ownership change Admin On Rejected",
                            Argument = "",
                            ForEntityId = assetTransfer.AssetId,
                            TypeEntityId = assetTransfer.AssetOwnershipTransferId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }

                return command.AssetTransferId;

            }
            else
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            }



        }

    }
}
