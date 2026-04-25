using Dapper;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using AuthModule.Application.DTOs.Lookup;
using Serilog;
using JustGo.Authentication.Infrastructure.Logging;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetMyAssets;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetMyLeases;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using System.Data.Common;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetLeaseStatusCommands
{
    public class ChangeAssetLeaseStatusCommandHandler : IRequestHandler<ChangeAssetLeaseStatusCommand, string>
    {

        private readonly IMediator _mediator;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IUnitOfWork _unitOfWork;

        public ChangeAssetLeaseStatusCommandHandler(
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

        public async Task<string> Handle(ChangeAssetLeaseStatusCommand command, CancellationToken cancellationToken)
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


        private async Task<string> ChangeStatus(ChangeAssetLeaseStatusCommand command, int currentUserId, CancellationToken cancellationToken)
        {

            var isInMyleasesInOwnerSide = await _mediator.Send(new GetMyLeasesQuery()
            {
                PageNumber = 1,
                PageSize = 1,
                SearchItems = new List<SearchSegmentDTO>() {
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseRecordGuid",
                       FieldId = "",
                       Operator = "equals",
                       Value = command.AssetLeaseId,
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

            var isInMyleasesInLeaseeSide = await _mediator.Send(new GetMyLeasesQuery()
            {
                PageNumber = 1,
                PageSize = 1,
                SearchItems = new List<SearchSegmentDTO>() {
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseRecordGuid",
                       FieldId = "",
                       Operator = "equals",
                       Value = command.AssetLeaseId,
                       ConditionJoiner = "and"

                   },
                   new SearchSegmentDTO()
                   {
                       ColumnName  = "LeaseIn",
                       FieldId = "",
                       Operator = "equals",
                       Value = "1",
                       ConditionJoiner = ""

                   }
                }
            });

            int statusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status = command.Status });
            int pendingConfirmationStatusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status = LeaseStatusType.PendingConfirmation });
            int pendingPaymentStatusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status = LeaseStatusType.PendingPayment });
            int pendingApprovalStatusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status = LeaseStatusType.PendingApproval });
            int activeStatusId = await _mediator.Send(new GetLeaseStatusIdQuery() { Status = LeaseStatusType.Active });

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", command.AssetLeaseId);
            queryParameters.Add("@CurrentUserId", currentUserId);


            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
           .GetAsync($@"Select * from AssetTypes Where AssetTypeId = (
                            select AssetTypeId from AssetRegisters where AssetId = (
                                Select AssetId from AssetLeases where RecordGuid = @RecordGuid
                            )
                        )", cancellationToken, queryParameters, null, "text");



            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);


            var assetLease = await _readRepository.GetLazyRepository<AssetLease>().Value
                    .GetAsync($@"select * from AssetLeases Where RecordGuid = @RecordGuid", cancellationToken, queryParameters, null, "text");

            var leaseesQParams = new DynamicParameters();
            leaseesQParams.Add("@AssetLeaseId", assetLease.AssetLeaseId);

            var leasees = await _readRepository.GetLazyRepository<AssetOwnership>().Value
                        .GetListAsync($@"select * from AssetOwnerships Where EntityId = @AssetLeaseId and EntityType = 2 and OwnerType != 3", cancellationToken, leaseesQParams, null, "text");

            var ownersQParams = new DynamicParameters();
            ownersQParams.Add("@AssetId", assetLease.AssetId);

            var owners = await _readRepository.GetLazyRepository<AssetOwnership>().Value
                        .GetListAsync($@"select * from AssetOwnerships Where AssetId = @AssetId and EntityType = 1 and OwnerType != 3", cancellationToken, ownersQParams, null, "text");


            if (
                ((leasees.Any(r => r.OwnerId == currentUserId) ||
                 isInMyleasesInLeaseeSide.TotalCount > 0 ||
                 (await HierarkeyCheck(leasees.Select(r => r.OwnerId ?? 0).ToList(), currentUserId, cancellationToken))) &&
               command.Status == LeaseStatusType.PendingApproval &&
               assetLease.StatusId == pendingConfirmationStatusId)  
               //leasee or family or admin can confirm

               ||

               ((leasees.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) ||
                 isInMyleasesInLeaseeSide.TotalCount > 0 ||
                 (await HierarkeyCheck(leasees.Select(r => r.OwnerId ?? 0).ToList(), currentUserId, cancellationToken) ||

                 owners.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) || 
                 isInMyleasesInOwnerSide.TotalCount > 0 ||
                 (await HierarkeyCheck(owners.Select(r => r.OwnerId ?? 0).ToList(), currentUserId, cancellationToken)))) &&
               command.Status == LeaseStatusType.Rejected &&
               (assetLease.StatusId == pendingConfirmationStatusId ||
                assetLease.StatusId == pendingPaymentStatusId) //&&
                //command.RejectionReason != null
                )//leasee/owner or their family or admin can reject

               ||

               ((owners.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) || 
                 isInMyleasesInOwnerSide.TotalCount > 0) &&
                command.Status == LeaseStatusType.Cancelled) // owner or family member can cancel at anytime

               ||

               (
                (await HierarkeyCheck(leasees.Select(r => r.OwnerId??0).ToList(), currentUserId, cancellationToken)) &&
                command.Status == LeaseStatusType.Cancelled) // admin can cancel at anytime
              )

            {

                if (command.Status == LeaseStatusType.PendingApproval)
                {
                    queryParameters = new DynamicParameters();
                    queryParameters.Add("@AssetTypeId", assetType.AssetTypeId);
                    var workflowStep = await _readRepository.GetLazyRepository<WorkflowStep>().Value
                        .GetAsync($@"Select Top(1) * from WorkflowSteps Where AssetTypeId = @AssetTypeId and WorkFlowType = 4", cancellationToken, queryParameters, null, "text");

                    if(workflowStep == null)
                    {
                        statusId = activeStatusId;
                    }

                }
                else if (command.Status == LeaseStatusType.Rejected)
                {
                    assetLease.RejectionReason = command.RejectionReason??RejectionReason.None;
                    assetLease.RecordRemarks = command.RejectionNote;

                }

                //var dbTransaction = await _unitOfWork.BeginTransactionAsync();

                assetLease.SetUpdateInfo(currentUserId);
                assetLease.StatusId = statusId;

                var (sql, qparams) = SQLHelper
                    .GenerateUpdateSQLWithParameters(assetLease, "RecordGuid",
                    new string[] { "AssetLeaseId", "AssetId", "CreatedBy", "CreatedDate", "RecordStatus" },
                    "AssetLeases");

                //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(sql, qparams, null, "text");


                queryParameters = new DynamicParameters();
                queryParameters.Add("@CurrentUserId", currentUserId);
                queryParameters.Add("@AssetLeaseId", assetLease.AssetLeaseId);
                queryParameters.Add("@LeaseAssetId", assetLease.AssetId);

                //await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @CurrentUserId, @AssetId = @LeaseAssetId, @LeaseId = @AssetLeaseId, @AssetLicenseId = null",
                //                                        cancellationToken, queryParameters, dbTransaction, "text");

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @CurrentUserId, @AssetId = @LeaseAssetId, @LeaseId = @AssetLeaseId, @AssetLicenseId = null",
                                        cancellationToken, queryParameters, null, "text");

                //await _unitOfWork.CommitAsync(dbTransaction);

                int action = 0;
                string actionName = "";

                if(command.Status == LeaseStatusType.Cancelled)
                {
                    action = AuditScheme.AssetManagement.AssetLease.Cancelled.Value;
                    actionName = AuditScheme.AssetManagement.AssetLease.Cancelled.Name;
                }
                else if (command.Status == LeaseStatusType.Rejected)
                {
                    action = AuditScheme.AssetManagement.AssetLease.Rejected.Value;
                    actionName = AuditScheme.AssetManagement.AssetLease.Rejected.Name;
                }
                else if (command.Status == LeaseStatusType.PendingApproval)
                {
                    action = AuditScheme.AssetManagement.AssetLease.ChangeStatus.Value;
                    actionName = AuditScheme.AssetManagement.AssetLease.ChangeStatus.Name;
                }


                CustomLog.Event(AuditScheme.AssetManagement.Value,
                   AuditScheme.AssetManagement.AssetLease.Value,
                   action,
                   currentUserId,
                   assetLease.AssetLeaseId,
                   LogEntityType.Asset,
                   assetLease.AssetLeaseId,
                   actionName,
                   "Asset lease status changed;" + JsonConvert.SerializeObject(assetLease)
                  );


                if(command.Status == LeaseStatusType.Rejected)
                {
                    if(leasees.Any(r => r.OwnerId == currentUserId && r.OwnerType == OwnerType.Individual) ||
                       isInMyleasesInLeaseeSide.TotalCount > 0)
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Lessee rejected lease",
                            Argument = "",
                            ForEntityId = assetLease.AssetId,
                            TypeEntityId = assetLease.AssetLeaseId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Admin rejected lease",
                            Argument = "",
                            ForEntityId = assetLease.AssetId,
                            TypeEntityId = assetLease.AssetLeaseId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }

                return command.AssetLeaseId;

            }
            else
            {
                throw new ForbiddenAccessException("Invalid Attempt!");
            }



        }

    }
}
