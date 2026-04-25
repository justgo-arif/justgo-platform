using System.Data;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetActionAssets;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.AssetManagement.Application.Features.Workflows.Commands.WorkflowSubmissions;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using Newtonsoft.Json;
using EntityType = JustGo.AssetManagement.Domain.Entities.Enums.EntityType;
using LogAuditScheme = JustGo.Authentication.Infrastructure.Logging;
using LogEntityType = JustGo.Authentication.Infrastructure.Logging.EntityType;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.Common.Helpers;
using JustGo.AssetManagement.Application.Features.AssetEmail.Commands.SendAssetEmail;
using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;



namespace JustGo.WorkflowManagement.Application.Features.Workflows.Commands.WorkflowSubmissions
{
    public class WorkflowSubmissionCommandHandler : IRequestHandler<WorkflowSubmissionCommand, WorkflowResponseDTO>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public WorkflowSubmissionCommandHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IWriteRepositoryFactory writeRepository,
            IUnitOfWork unitOfWork,
            IUtilityService utilityService)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<WorkflowResponseDTO> Handle(WorkflowSubmissionCommand command, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            int proxyUserId = !string.IsNullOrEmpty(command.ProxyUserId) ?
                                (await _mediator.Send(new GetIdByGuidQuery()
                                {
                                    Entity = AssetTables.User,
                                    RecordGuids = new List<string>() { command.ProxyUserId }
                                })
                                )[0] : 0;
            return await ChangeEntityStatus(currentUserId, proxyUserId, command, cancellationToken);

        }
        private async Task<int> SaveWorkflow(int currentUserId, WorkflowSubmissionCommand command, int entityId, int stepId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {

            var req = new WorkflowEntity()
            {
                ActionStatus = command.ActionStatus,
                RejectionReason = command.RejectionReason,
                Remarks = command.Remarks,
            };

            req.SetCreateInfo(currentUserId);
            req.StepId = stepId;
            req.EntityId = entityId;
            req.UserId = currentUserId;
            req.ActionDate = req.CreatedDate;

            var (sql, qparams) = SQLHelper
                .GenerateInsertSQLWithParameters(req,
                new string[] { "WorkflowEntityId", "RecordGuid" },
                "WorkflowEntities");

            var result = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().Value.ExecuteMultipleAsync(sql, cancellationToken, qparams, dbTransaction, "text");

            if (result != null)
            {
                req.WorkflowEntityId = result.Id;

            }


            return req.WorkflowEntityId;
        }

        private async Task ApproveAsset(int currentUserId, int entityId, CancellationToken cancellationToken, IDbTransaction dbTransaction)

        {


            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@currentUserId", currentUserId);
            dynamicParameters.Add("@entityId", entityId);

            await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                                $@"Update AssetRegisters 
                                    set RecordChangedBy = @currentUserId,
                                        RecordChangedDate = GETUTCDATE(), 
                                        StatusId = 
                                          (select AssetStatusId from AssetStatus 
                                           Where Type = {(int)EntityType.Asset} and
                                                 Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.Active))}')
                                   Where
                                    AssetId = @entityId and 
                                    StatusId in 
                                          (select AssetStatusId from AssetStatus 
                                           Where Type = {(int)EntityType.Asset} and
                                                ( Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.PendingApproval))}'
                                                  Or
                                                  Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.UnderReview))}'
                                                ))"
                                       , cancellationToken, dynamicParameters, dbTransaction, "text");


        }


        private async Task<bool> checkAuthorization(
            AssetRegister asset, 
            AssetType assetType,
            WorkflowSubmissionCommand command, 
            WorkflowStep currentStep,
            int currentUserId,
            int proxyUserId,
            CancellationToken cancellationToken)
        {

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);
            var leaseConfig = JsonConvert.DeserializeObject<AssetLeaseConfig>(assetType.AssetLeaseConfig);
            var transferConfig = JsonConvert.DeserializeObject<AssetTransferConfig>(assetType.AssetTransferConfig);

            if (command.WorkFlowType == WorkFlowType.License)
            {


                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@EntityId", command.EntityId);


                string checkOwnerSql = @"With 
	                        DefaultAdminRoles as (
	                        Select top 1 arl.Id Id 
	                        from AbacRoles arl
	                        inner join AbacUserRoles aurl on aurl.RoleId = arl.Id
	                        where 
	                        aurl.UserId = @CurrentUserId and
	                        arl.Name in(
		                        'System Admin',
		                        'Asset Super Admin'
		                        )
	                        ),
	                        AdminRoleStates as (
	                        Select aurl.OrganizationId OrganizationId
	                        from AbacRoles arl
	                        inner join AbacUserRoles aurl on aurl.RoleId = arl.Id  
	                        where
	                        aurl.UserId = @CurrentUserId and
	                        arl.Name = 'Asset Admin'
	                        )
                            Select 
                            'Permission' [Text],
                            case 
	                            when ars.OrganizationId is not null  then 1
	                            when exists(select * from DefaultAdminRoles) then 1
	                            else 0
                            end [Value]
                            from
                            AssetLicenses ali 
                            inner join Products_Default pd on pd.DocId = ali.ProductId
                            left  join AdminRoleStates ars on ars.OrganizationId = pd.Ownerid
                            where ali.RecordGuid = @EntityId ";

                if (!typeConfig.ApproveLicenseByOwnerOnly)
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
                               Value = asset.RecordGuid,
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


                    var isOwnerAdmin = await _readRepository.GetLazyRepository<SelectListItemDTO<bool>>().Value
                                        .GetAsync(checkOwnerSql,
                                          cancellationToken, dynamicParameters, null, "text");

                    if (IsInAssetList.TotalCount == 0 && !isOwnerAdmin.Value)
                    {
                        return false;
                    }
                }
                else
                {
                    var isOwnerAdmin = await _readRepository.GetLazyRepository<SelectListItemDTO<bool>>().Value
                    .GetAsync(checkOwnerSql,
                      cancellationToken, dynamicParameters, null, "text");

                    if (!isOwnerAdmin.Value)
                    {
                        return false;
                    }
                }



            }
            else if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval ||
                     command.WorkFlowType == WorkFlowType.OwnerTransferApproval
                      )
            {
                var qOwnersParameters = new DynamicParameters();
                qOwnersParameters.Add("@AssetId", asset.AssetId);
                qOwnersParameters.Add("@proxyUserId", proxyUserId);
                qOwnersParameters.Add("@currentUserId", currentUserId);

                if (proxyUserId == 0)
                {
                    qOwnersParameters.Add("@userId", currentUserId);
                }
                else
                {
                    qOwnersParameters.Add("@userId", proxyUserId);
                }

                    AssetOwner IsOwner = await _readRepository.GetLazyRepository<AssetOwner>().Value
                                        .GetAsync($@"Select Top 1 * from AssetOwners 
                                                                Where AssetId = @AssetId and 
                                                                OwnerId = @userId",
                                          cancellationToken, qOwnersParameters, null, "text");

                var isFamilyProxy = await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value
                        .GetAsync(@"Select Top 1 u.FirstName [Text], u.UserId [Value]
                            FROM [User] u
						    inner join Family_Links fm on fm.Entityid = u.MemberDocId
                            Where 
                            (u.UserId = @proxyUserId
                                and
                                fm.Docid in (Select DocId  
                                    from  Family_Links fl where fl.Entityid = (
                                    Select MemberDocId from [User]
                                    where userid = @currentUserId)))",
                          cancellationToken, qOwnersParameters, null, "text");

                if(IsOwner == null && proxyUserId == 0)
                {
                    return false;
                }
                else if (IsOwner == null && 
                     proxyUserId != 0 &&
                     isFamilyProxy == null)
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
                               Value = asset.RecordGuid,
                               ConditionJoiner = "and"

                           },
                           new SearchSegmentDTO()
                           {
                               ColumnName  = "LeaseIn",
                               FieldId = "",
                               Operator = "not equals",
                               Value = "1",
                               ConditionJoiner = ""
                           },
                           new SearchSegmentDTO()
                           {
                               ColumnName  = "UserId",
                               FieldId = "",
                               Operator = "equals",
                               Value = command.ProxyUserId??"0",
                               ConditionJoiner = ""
                           }
                        }
                    });

                    if (IsInAssetList.TotalCount == 0)
                    {
                        return false;
                    }
                }


            }
            else if (command.WorkFlowType == WorkFlowType.Lease)
            {

                var qOwnersParameters = new DynamicParameters();
                qOwnersParameters.Add("@EntityId", command.EntityId);

                int OwnerClubId = 0;

                if (leaseConfig.AllowedPayment)
                {

                    AssetLease lease = await _readRepository.GetLazyRepository<AssetLease>().Value
                    .GetAsync($@"Select Top 1 * from AssetLeases 
                                                Where RecordGuid = @EntityId",
                        cancellationToken, qOwnersParameters, null, "text");

                    OwnerClubId = lease.OwnerClubId ?? 0;
                }

                var searchItems = new List<SearchSegmentDTO>() {
                        new SearchSegmentDTO()
                        {
                            ColumnName  = "AssetRecordGuid",
                            FieldId = "",
                            Operator = "equals",
                            Value = asset.RecordGuid,
                            ConditionJoiner = "and"

                        },
                        new SearchSegmentDTO()
                        {
                            ColumnName  = "LeaseIn",
                            FieldId = "",
                            Operator = "equals",
                            Value = "1",
                            ConditionJoiner = "and"

                        }

                    };

                if (leaseConfig.AllowedPayment)
                {
                    searchItems.Add(new SearchSegmentDTO()
                    {
                        ColumnName = "ClubDocId",
                        FieldId = "",
                        Operator = "equals",
                        Value = OwnerClubId.ToString(),
                        ConditionJoiner = ""
                    });
                }

                var IsInAssetList = await _mediator.Send(new GetAssetsQuery()
                {
                    PageNumber = 1,
                    PageSize = 1,
                    SearchItems = searchItems
                });

                if (IsInAssetList.TotalCount == 0)
                {
                    return false;
                }

            }
            else if (command.WorkFlowType == WorkFlowType.Transfer)
            {

               /* var qOwnersParameters = new DynamicParameters();
                qOwnersParameters.Add("@EntityId", command.EntityId);

                int OwnerClubId = 0;

                if (transferConfig.AllowedPayment)
                {
                    AssetOwnershipTransfer transfer = await _readRepository.GetLazyRepository<AssetOwnershipTransfer>().Value
                    .GetAsync($@"Select Top 1 * from AssetOwnershipTransfers 
                                            Where RecordGuid = @EntityId",
                        cancellationToken, qOwnersParameters, null, "text");

                    OwnerClubId = transfer.OwnerClubId ?? 0;
                }

                var searchItems = new List<SearchSegmentDTO>() {
                        new SearchSegmentDTO()
                        {
                            ColumnName  = "AssetRecordGuid",
                            FieldId = "",
                            Operator = "equals",
                            Value = asset.RecordGuid,
                            ConditionJoiner = "and"

                        },
                        new SearchSegmentDTO()
                        {
                            ColumnName  = "LeaseIn",
                            FieldId = "",
                            Operator = "not equals",
                            Value = "1",
                            ConditionJoiner = "and"

                        }

                    };

                if (transferConfig.AllowedPayment)
                {
                    searchItems.Add(new SearchSegmentDTO()
                    {
                        ColumnName = "ClubDocId",
                        FieldId = "",
                        Operator = "equals",
                        Value = OwnerClubId.ToString(),
                        ConditionJoiner = ""
                    });
                }

                var IsInAssetList = await _mediator.Send(new GetAssetsQuery()
                {
                    PageNumber = 1,
                    PageSize = 1,
                    SearchItems = searchItems
                });

                if (IsInAssetList.TotalCount == 0)
                {
                    return false;
                }*/

                return true; //validated based on abac hence skipped here.

            }
            else
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
                               Value = asset.RecordGuid,
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

                if (IsInAssetList.TotalCount == 0)
                {
                    return false;
                }

            }


            if (currentStep.AuthorityType == AuthorityType.Individual && 
                (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval ||
                command.WorkFlowType == WorkFlowType.OwnerTransferApproval) &&
                currentStep.AuthorityId != currentUserId &&
                currentStep.AuthorityId != proxyUserId
                )
            {
                return false;
            }
            else if(command.WorkFlowType != WorkFlowType.OwnerLeaseApproval &&
                    command.WorkFlowType != WorkFlowType.OwnerTransferApproval
                    )
            {
                if (currentStep.AuthorityType == AuthorityType.Individual &&
                     currentStep.AuthorityId != currentUserId)
                {
                    return false;
                }
                else
                {

                    var adminRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                  .Value.GetListAsync($@"select [Name] [Text], Id [Value] from AbacRoles 
                                                    where Name in(
                                                    'System Admin',
                                                    'Asset Super Admin'
                                                    )", cancellationToken, null, null, "text")).ToList();


                    var authorRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                                      .Value.GetListAsync($@"select [Name] [Text], Id [Value] from AbacRoles 
                                                    where Name in(
                                                    'Asset Admin',
                                                    'Asset Manager'
                                                    )", cancellationToken, null, null, "text")).ToList();


                    DynamicParameters dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@adminRoles", adminRoles.Select(r => r.Value).ToList());
                    dynamicParameters.Add("@authorRoles", authorRoles.Select(r => r.Value).ToList());
                    dynamicParameters.Add("@currentUserId", currentUserId);
                    dynamicParameters.Add("@AuthorityId", currentStep.AuthorityId);

                    var IsUserAuthorityCount = await _readRepository.GetLazyRepository<CountDTO>().Value.GetAsync(
                                $@"select Count(distinct hl.UserId) TotalRowCount from 
                                HierarchyLinks hl
                                Inner join Hierarchies h  on  h.[Id] = hl.[HierarchyId]
							    Left join AbacUserRoles abcr  on  abcr.UserId = hl.UserId and abcr.OrganizationId = h.EntityId
							    where
                                hl.UserId = @currentUserId 
                                and
                                (
								    abcr.RoleId in @adminRoles
                                    or
                                    (
								        h.HierarchyTypeId = @AuthorityId 
								        and 
                                        abcr.RoleId in @authorRoles
                                    )
                                )",
                                cancellationToken, dynamicParameters, null, "text");

                    if (IsUserAuthorityCount.TotalRowCount == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private DateTime GetCurrentDate()
        {
            return DateTime.Today;
        }


        private async Task<int> processWorkflowStep(
            WorkflowSubmissionCommand command,
            int action,
            string actionName,
            int subCategory,
            WorkflowStep currentStep,
            int currentUserId,
            int entityId,
            int ResourceId,
            AssetRegister asset,
            AssetType assetType,
            IDbTransaction dbTransaction,
            CancellationToken cancellationToken,
            bool IsLastStep)
        {
            var hasTransferTo = false;
            var expiredLicenses = new List<AssetLicense>();

            if (command.WorkFlowType == WorkFlowType.AssetReview)
            {

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);



                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {


                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetRegisters 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.PendingApproval))}')
                               Where
                                AssetId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.UnderReview))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {



                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetRegisters 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.Suspended))}')
                               Where
                                AssetId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.UnderReview))}')"
                        , cancellationToken, dynamicParameters, dbTransaction, "text");
                }

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @entityId, @LeaseId = null, @AssetLicenseId = null",
                                                                        cancellationToken, dynamicParameters, dbTransaction, "text");
            }
            else if (command.WorkFlowType == WorkFlowType.AssetApprove)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    await ApproveAsset(currentUserId, entityId, cancellationToken, dbTransaction);
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetRegisters 
                                set RecordChangedBy = @currentUserId,
                                    RecordChangedDate = GETUTCDATE(),
                                    StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.Suspended))}')
                               Where
                                AssetRegisterId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Asset} and
                                             Name = '{(Utilities.GetEnumText<AssetStatusType>(AssetStatusType.PendingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @entityId, @LeaseId = null, @AssetLicenseId = null",
                                                                        cancellationToken, dynamicParameters, dbTransaction, "text");
            }
            else if (command.WorkFlowType == WorkFlowType.Lease)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                var lease = await _readRepository.GetLazyRepository<AssetLease>().Value
                        .GetAsync($@"Select * from AssetLeases al where al.AssetLeaseId = @entityId",
                         cancellationToken, dynamicParameters, null, "text");


                var targetStatus = LeaseStatusType.Active;

                if (lease.LeaseStartDate > GetCurrentDate())
                {
                    targetStatus = LeaseStatusType.Scheduled;

                }


                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLeases
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(targetStatus))}')
                               Where
                                AssetLeaseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLeases 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.Rejected))}')
                               Where
                                AssetLeaseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingApproval))}')"
                        , cancellationToken, dynamicParameters, dbTransaction, "text");
                }



                dynamicParameters.Add("@LeaseAssetId", asset.AssetId);

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @LeaseAssetId, @LeaseId = @entityId, @AssetLicenseId = null",
                                                                        cancellationToken, dynamicParameters, dbTransaction, "text");
            }
            else if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    var leaseConfig = JsonConvert.DeserializeObject<AssetLeaseConfig>(assetType.AssetLeaseConfig);

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLeases
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(leaseConfig.AllowedPayment ?
                                                       Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingPayment) :
                                                       Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingConfirmation))}')
                               Where
                                AssetLeaseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingOwnerApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLeases 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.Rejected))}')
                               Where
                                AssetLeaseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Lease} and
                                             Name = '{(Utilities.GetEnumText<LeaseStatusType>(LeaseStatusType.PendingOwnerApproval))}')"
                        , cancellationToken, dynamicParameters, dbTransaction, "text");
                }



                dynamicParameters.Add("@LeaseAssetId", asset.AssetId);

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @LeaseAssetId, @LeaseId = @entityId, @AssetLicenseId = null",
                                                                        cancellationToken, dynamicParameters, dbTransaction, "text");
            }
            else if (command.WorkFlowType == WorkFlowType.Transfer)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);


                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetOwnershipTransfers
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.Completed))}')
                               Where
                                AssetOwnershipTransferId = @entityId and 
                                TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");


                    var sql = $@"select *
                        from AssetTransferOwners ao
                        WHERE ao.AssetOwnershipTransferId = @EntityId ";

                    var queryParameters = new DynamicParameters();
                    queryParameters.Add("@EntityId", entityId);

                    var atoResult = (await _readRepository.GetLazyRepository<AssetTransferOwner>()
                             .Value.GetListAsync(sql, cancellationToken, queryParameters,
                                 null, "text")).ToList();


                    queryParameters = new DynamicParameters();
                    queryParameters.Add("@AssetId", asset.AssetId);
                    await _writeRepository.GetLazyRepository<object>().Value.
                            ExecuteAsync($@"delete from AssetOwners where AssetId = @AssetId",
                            cancellationToken,
                            queryParameters,
                            dbTransaction, "text");

                    await _writeRepository.GetLazyRepository<object>().Value.
                        ExecuteAsync($@"delete from AssetOwnerships
                              where AssetId = @AssetId and
                              EntityType = 1",
                        cancellationToken,
                        queryParameters,
                        dbTransaction, "text");

                    foreach (var item in atoResult)
                    {


                        var owner = new AssetOwner()
                        {
                            AssetId = asset.AssetId,
                            OwnerId = item.OwnerId,
                            OwnerTypeId = item.OwnerType
                        };

                        owner.SetCreateInfo(currentUserId);

                        var (aoiSql, qparams) = SQLHelper.GenerateInsertSQLWithParameters(
                            owner, new string[] { "AssetOwnerId", "RecordGuid" }, "AssetOwners");

                        var aoiResult = await _writeRepository.GetLazyRepository<InsertedDataIdDTO>().
                                            Value.ExecuteMultipleAsync(
                                            aoiSql, cancellationToken, qparams, dbTransaction, "text");

                        var ownerShip = new AssetOwnership()
                        {
                            AssetId = asset.AssetId,
                            OwnerId = item.OwnerId,
                            OwnerType = item.OwnerType,
                            EntityId = aoiResult.Id,
                            EntityType = OwnershipEntityType.AssetOwner

                        };

                        var (sql2, qparams2) = SQLHelper.GenerateInsertSQLWithParameters(
                            ownerShip, new string[] { "AssetOwnershipId" }, "AssetOwnerships");

                        await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                            sql2, cancellationToken, qparams2, dbTransaction, "text");

                    }


                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetOwnershipTransfers 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.Rejected))}')
                               Where
                                AssetOwnershipTransferId = @entityId and 
                                TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingApproval))}')"
                        , cancellationToken, dynamicParameters, dbTransaction, "text");
                }


                dynamicParameters.Add("@TransferAssetId", asset.AssetId);
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @TransferAssetId, @LeaseId = null, @AssetLicenseId = null",
                                                        cancellationToken, dynamicParameters, dbTransaction, "text");

            }
            else if (command.WorkFlowType == WorkFlowType.OwnerTransferApproval)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {

                    var sql = $@"select top 1 *
                                    from 
                                    AssetTransferOwners ao
                                    WHERE ao.AssetOwnershipTransferId = @entityId ";

                    var queryParameters = new DynamicParameters();
                    queryParameters.Add("@entityId", entityId);

                    hasTransferTo = (await _readRepository.GetLazyRepository<AssetTransferOwner>()
                                         .Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text"))
                                        .Any();


                    var transferConfig = JsonConvert.DeserializeObject<AssetTransferConfig>(assetType.AssetTransferConfig);

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetOwnershipTransfers
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(!hasTransferTo ?
                                                       Utilities.GetEnumText<TransferStatusType>(TransferStatusType.Completed) :
                                                       transferConfig.AllowedPayment ?
                                                       Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingPayment) :
                                                       Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingConfirmation))
                                                       }')
                               Where
                                AssetOwnershipTransferId = @entityId and 
                                TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingOwnerApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");


                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetOwnershipTransfers 
                                set  RecordChangedBy = @currentUserId,
                                     RecordChangedDate = GETUTCDATE(),
                                     TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.Rejected))}')
                               Where
                                AssetOwnershipTransferId = @entityId and 
                                TransferStatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Transfer} and
                                             Name = '{(Utilities.GetEnumText<TransferStatusType>(TransferStatusType.PendingOwnerApproval))}')"
                        , cancellationToken, dynamicParameters, dbTransaction, "text");
                }


                dynamicParameters.Add("@TransferAssetId", asset.AssetId);
                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @TransferAssetId, @LeaseId = null, @AssetLicenseId = null",
                                                        cancellationToken, dynamicParameters, dbTransaction, "text");


            }
            else if (command.WorkFlowType == WorkFlowType.License)
            {
                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLicenses 
                                set RecordChangedBy = @currentUserId,
                                    RecordChangedDate = GETUTCDATE(),
                                    StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.License} and
                                             Name = '{(Utilities.GetEnumText<LicenseStatusType>(LicenseStatusType.Active))}')
                               Where
                                AssetLicenseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.License} and
                                             Name = '{(Utilities.GetEnumText<LicenseStatusType>(LicenseStatusType.AwaitingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");



                    var expireLicensesSql = $@"select al.*
                                            from 
                                            AssetLicenses al
                                            inner join Products_Links pl on pl.DocId = al.ProductId
                                            inner join AssetTypesLicenseLink atll on atll.SourceUpgradeLicense = pl.Entityid
                                            where al.StatusId = 8 and atll.LicenseDocId = {ResourceId} and al.AssetId = {asset.AssetId}";

                    expiredLicenses = (await _readRepository.GetLazyRepository<AssetLicense>().Value.GetListAsync(expireLicensesSql,
                                             cancellationToken, null, dbTransaction, "text")).ToList();

                    var expirySql = $@"update al
                                            set StatusId = 9
                                            from 
                                            AssetLicenses al
                                            inner join Products_Links pl on pl.DocId = al.ProductId
                                            inner join AssetTypesLicenseLink atll on atll.SourceUpgradeLicense = pl.Entityid
                                            where al.StatusId = 8 and atll.LicenseDocId = {ResourceId} and al.AssetId = {asset.AssetId}";

                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(expirySql,
                        cancellationToken, null, dbTransaction, "text");
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetLicenses 
                                set RecordChangedBy = @currentUserId,
                                    RecordChangedDate = GETUTCDATE(),
                                    StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.License} and
                                             Name = '{(Utilities.GetEnumText<LicenseStatusType>(LicenseStatusType.Suspended))}')
                               Where
                                AssetLicenseId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.License} and
                                             Name = '{(Utilities.GetEnumText<LicenseStatusType>(LicenseStatusType.AwaitingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }



                dynamicParameters.Add("@LicenseAssetId", asset.AssetId);

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @LicenseAssetId, @LeaseId = null, @AssetLicenseId = @entityId",
                                                    cancellationToken, dynamicParameters, dbTransaction, "text");


            }
            else if (command.WorkFlowType == WorkFlowType.Credential)
            {

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@currentUserId", currentUserId);
                dynamicParameters.Add("@entityId", entityId);

                if (command.ActionStatus == ActionStatus.Approve && IsLastStep)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetCredentials 
                                set RecordChangedBy = @currentUserId,
                                    RecordChangedDate = GETUTCDATE(),
                                    StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Credential} and
                                             Name = '{(Utilities.GetEnumText<CredentialStatusType>(CredentialStatusType.Active))}')
                               Where
                                AssetCredentialId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Credential} and
                                             Name = '{(Utilities.GetEnumText<CredentialStatusType>(CredentialStatusType.AwaitingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }
                else if (command.ActionStatus == ActionStatus.Reject)
                {
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                        $@"Update AssetCredentials 
                                set RecordChangedBy = @currentUserId,
                                    RecordChangedDate = GETUTCDATE(),
                                    StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Credential} and
                                             Name = '{(Utilities.GetEnumText<CredentialStatusType>(CredentialStatusType.Suspended))}')
                               Where
                                AssetCredentialId = @entityId and 
                                StatusId = 
                                      (select AssetStatusId from AssetStatus 
                                       Where Type = {(int)EntityType.Credential} and
                                             Name = '{(Utilities.GetEnumText<CredentialStatusType>(CredentialStatusType.AwaitingApproval))}')"
                    , cancellationToken, dynamicParameters, dbTransaction, "text");
                }



                dynamicParameters.Add("@CredentialAssetId", asset.AssetId);

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync($@"exec ResolveReasonsForAssetId @UserId = @currentUserId, @AssetId = @CredentialAssetId, @LeaseId = null, @AssetLicenseId = null",
                                                    cancellationToken, dynamicParameters, dbTransaction, "text");



            }

            var result = await SaveWorkflow(currentUserId, command, entityId, currentStep.StepId, dbTransaction, cancellationToken);

            await _unitOfWork.CommitAsync(dbTransaction);


            if (command.WorkFlowType == WorkFlowType.License ||
               command.WorkFlowType == WorkFlowType.Credential)
            {
                if (AssetStatusHelper.checkIsActionStatusId(asset.StatusId))
                {
                    await _mediator.Send(new AssetStateAllocationCommand()
                    {
                        AssetRegisterId = asset.RecordGuid
                    });

                }
            }


            if (command.WorkFlowType == WorkFlowType.License)
            {
                foreach (var item in expiredLicenses)
                {

                    CustomLog.Event(LogAuditScheme.AuditScheme.AssetManagement.Value,
                        LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Value,
                        LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Updated.Value,
                       currentUserId,
                       item.AssetLicenseId,
                       LogEntityType.Asset,
                       item.AssetId,
                       LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Updated.Name,
                       "License Updated;" + JsonConvert.SerializeObject(command)
                      );

                }
            }


            if (result != 0)
            {
                if (command.ActionStatus == ActionStatus.Approve)
                {

                    CustomLog.Event(LogAuditScheme.AuditScheme.AssetManagement.Value,
                        subCategory,
                        action,
                       currentUserId,
                       entityId,
                       LogEntityType.Asset,
                       result,
                       actionName,
                       "Workflow submitted;" + JsonConvert.SerializeObject(command)
                      );
                }
                else
                {
                    CustomLog.Event(LogAuditScheme.AuditScheme.AssetManagement.Value,
                        subCategory,
                        action,
                       currentUserId,
                       entityId,
                       LogEntityType.Asset,
                       result,
                       actionName,
                       "Workflow submitted;" + JsonConvert.SerializeObject(command)
                      );
                }

                if(command.WorkFlowType == WorkFlowType.License)
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/License Approval Notification",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,

                        }, cancellationToken);
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/License Rejection Notification",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,

                        }, cancellationToken);
                    }
                }
                else if((command.WorkFlowType == WorkFlowType.AssetApprove ||
                        command.WorkFlowType == WorkFlowType.AssetReview))
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Asset approved",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = asset.AssetId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,

                        }, cancellationToken);
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Asset rejected",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = asset.AssetId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,

                        }, cancellationToken);
                    }
                }
                else if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval)
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        if (IsLastStep)
                        {
                            await _mediator.Send(new SendAssetEmailCommand
                            {
                                MessageScheme = "Asset/All Owner approved lease request",
                                Argument = "",
                                ForEntityId = asset.AssetId,
                                TypeEntityId = entityId,
                                InvokeUserId = currentUserId,
                                OwnerId = 0,
                            }, cancellationToken);

                            await _mediator.Send(new SendAssetEmailCommand
                            {
                                MessageScheme = "Asset/Lessee pending action",
                                Argument = "",
                                ForEntityId = asset.AssetId,
                                TypeEntityId = entityId,
                                InvokeUserId = currentUserId,
                                OwnerId = 0,
                            }, cancellationToken);
                        }
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Owner rejected lease request",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }
                else if(command.WorkFlowType == WorkFlowType.Lease)
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        if (IsLastStep)
                        {
                            await _mediator.Send(new SendAssetEmailCommand
                            {
                                MessageScheme = "Asset/Admin approved lease",
                                Argument = "",
                                ForEntityId = asset.AssetId,
                                TypeEntityId = entityId,
                                InvokeUserId = currentUserId,
                                OwnerId = 0,
                            }, cancellationToken);
                        }
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Admin rejected lease",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }
                else if (command.WorkFlowType == WorkFlowType.OwnerTransferApproval)
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        if (IsLastStep)
                        {


                            if (!hasTransferTo)
                            {

                                await _mediator.Send(new ChangeAssetStatusCommand()
                                {
                                    AssetRegisterId = asset.RecordGuid,
                                    Status = AssetStatusType.Archived,
                                });

                                var queryParameters = new DynamicParameters();
                                queryParameters.Add("@AssetId", asset.AssetId);
                                await _writeRepository.GetLazyRepository<object>().Value.
                                        ExecuteAsync($@"delete from AssetOwners where AssetId = @AssetId",
                                        cancellationToken,
                                        queryParameters,
                                        dbTransaction, "text");

                                await _writeRepository.GetLazyRepository<object>().Value.
                                    ExecuteAsync($@"delete from AssetOwnerships
                                      where AssetId = @AssetId and
                                      EntityType = 1",
                                            cancellationToken,
                                            queryParameters,
                                            dbTransaction, "text");
                            }

                            await _mediator.Send(new SendAssetEmailCommand
                            {
                                MessageScheme = "Asset/Current Owner Change On All Approval",
                                Argument = "",
                                ForEntityId = asset.AssetId,
                                TypeEntityId = entityId,
                                InvokeUserId = currentUserId,
                                OwnerId = 0,
                            }, cancellationToken);

                            if (hasTransferTo)
                            {
                                await _mediator.Send(new SendAssetEmailCommand
                                {
                                    MessageScheme = "Asset/New Owner Pending Action Notification",
                                    Argument = "",
                                    ForEntityId = asset.AssetId,
                                    TypeEntityId = entityId,
                                    InvokeUserId = currentUserId,
                                    OwnerId = 0,
                                }, cancellationToken);

                            }
                        }
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Current Owner Change On Reject",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }
                else if (command.WorkFlowType == WorkFlowType.Transfer)
                {
                    if (command.ActionStatus == ActionStatus.Approve)
                    {
                        if (IsLastStep)
                        {
                            await _mediator.Send(new SendAssetEmailCommand
                            {
                                MessageScheme = "Asset/Ownership change Admin On Approved",
                                Argument = "",
                                ForEntityId = asset.AssetId,
                                TypeEntityId = entityId,
                                InvokeUserId = currentUserId,
                                OwnerId = 0,
                            }, cancellationToken);
                        }
                    }
                    else
                    {
                        await _mediator.Send(new SendAssetEmailCommand
                        {
                            MessageScheme = "Asset/Ownership change Admin On Rejected",
                            Argument = "",
                            ForEntityId = asset.AssetId,
                            TypeEntityId = entityId,
                            InvokeUserId = currentUserId,
                            OwnerId = 0,
                        }, cancellationToken);
                    }
                }


            }

            return result;
        }
      
       

        private async Task<WorkflowResponseDTO> ChangeEntityStatus(
            int currentUserId, 
            int proxyUserId,
            WorkflowSubmissionCommand command, 
            CancellationToken cancellationToken)
        {

            /*if (command.ActionStatus == ActionStatus.Reject &&
               command.RejectionReason == RejectionReason.None)
            {
                return WorkflowResponseDTO.Failed("Rejection Reason Required");
            }*/



            int action = 0;
            string actionName = "";
            int subCategory = 0;



            AssetRegister asset = null;

            int ResourceId = 0;

            var qTypeParameters = new DynamicParameters();
            qTypeParameters.Add("@RecordGuid", command.AssetTypeId);

            var assetType = await _readRepository.GetLazyRepository<AssetType>().Value
                  .GetAsync($@"Select * from AssetTypes Where RecordGuid = @RecordGuid", 
                  cancellationToken, qTypeParameters, null, "text");


            int AssetTypeId = assetType.AssetTypeId;

            int entityId = 0;

            if (command.WorkFlowType == WorkFlowType.AssetReview)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetRegisters
                }))[0];

                 DynamicParameters dynamicParameters = new DynamicParameters();
                 dynamicParameters.Add("@entityId", entityId);
                 asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                  .GetAsync($@"Select * from AssetRegisters 
                                            Where AssetId = @entityId", cancellationToken, dynamicParameters, null, "text");

                subCategory = LogAuditScheme.AuditScheme.AssetManagement.General.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.General.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.General.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.StatusChanged.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.StatusChanged.Name;
                }

            }
            else if (command.WorkFlowType == WorkFlowType.AssetApprove)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetRegisters
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);
                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                 .GetAsync($@"Select * from AssetRegisters 
                                            Where AssetId = @entityId", cancellationToken, dynamicParameters, null, "text");

                subCategory = LogAuditScheme.AuditScheme.AssetManagement.General.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.General.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.General.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.General.Approved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.General.Approved.Name;
                }

            }
            else if (command.WorkFlowType == WorkFlowType.Transfer)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetOwnershipTransfers
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                        .GetAsync($@"Select * from AssetRegisters ar where 
                            ar.AssetId = (Select atr.AssetId from AssetOwnershipTransfers atr where atr.AssetOwnershipTransferId = @entityId)",
                                         cancellationToken, dynamicParameters, null, "text");


                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Approved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Approved.Name;
                }


            }
            else if (command.WorkFlowType == WorkFlowType.OwnerTransferApproval)
            {
                //CustomLog.Event(LogAuditScheme.AuditScheme.AssetManagement.Value,
                //    LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Value,
                //    LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Created.Value,
                //   currentUserId,
                //   0,
                //   LogEntityType.Asset,
                //   0,
                //   LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Created.Name,
                //   "Workflow Change Status;" + JsonConvert.SerializeObject(command)
                //  );

                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetOwnershipTransfers
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                        .GetAsync($@"Select * from AssetRegisters ar where 
                            ar.AssetId = (Select atr.AssetId from AssetOwnershipTransfers atr where atr.AssetOwnershipTransferId = @entityId)",
                                         cancellationToken, dynamicParameters, null, "text");


                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.OwnerApproved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetTransfer.OwnerApproved.Name;
                }

                ResourceId = entityId;

            }
            else if (command.WorkFlowType == WorkFlowType.Lease)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetLeases
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                        .GetAsync($@"Select * from AssetRegisters ar where 
                            ar.AssetId = (Select al.AssetId from AssetLeases al where al.AssetLeaseId = @entityId)",
                                         cancellationToken, dynamicParameters, null, "text");


                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Approved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Approved.Name;
                }

            }
            else if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetLeases
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                        .GetAsync($@"Select * from AssetRegisters ar where 
                            ar.AssetId = (Select al.AssetId from AssetLeases al where al.AssetLeaseId = @entityId)",
                                         cancellationToken, dynamicParameters, null, "text");


                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.OwnerApproved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLease.OwnerApproved.Name;
                }

                ResourceId = entityId;

            }
            else if (command.WorkFlowType == WorkFlowType.License)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetLicenses
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                       .GetAsync($@"Select * from AssetRegisters 
                                            Where AssetId in (Select AssetId from AssetLicenses 
                                            Where AssetLicenseId = @entityId)"
                        , cancellationToken, dynamicParameters, null, "text");

                var resourceSql = $@"select 'Resource' [Text], ll.DocId [Value] from 
                                        License_Links ll
                                        inner join AssetLicenses al on al.ProductId = ll.Entityid
                                        where al.AssetLicenseId = @entityId";

                var resource = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value.
                               GetAsync(resourceSql, cancellationToken, dynamicParameters, null, "text"));

                if (resource != null)
                {
                    ResourceId = resource.Value;

                }

                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Approved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetLicense.Approved.Name;
                }

            }
            else if (command.WorkFlowType == WorkFlowType.Credential)
            {
                entityId = (await _mediator.Send(new GetIdByGuidQuery()
                {
                    RecordGuids = new List<string>() { command.EntityId },
                    Entity = AssetTables.AssetCredentials
                }))[0];

                DynamicParameters dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@entityId", entityId);

                asset = await _readRepository.GetLazyRepository<AssetRegister>().Value
                                           .GetAsync($@"Select * from AssetRegisters 
                                            Where AssetId in (Select AssetId from AssetCredentials 
                                            Where AssetCredentialId = @entityId)", cancellationToken, dynamicParameters, null, "text");

                var resourceSql = $@"select 'Resource' [Text], acr.CredentialMasterDocId [Value] from 
                                        AssetCredentials acr
                                        where acr.AssetCredentialId = @entityId";

                var resource = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>().Value.
                               GetAsync(resourceSql, cancellationToken, dynamicParameters, null, "text"));

                if (resource != null)
                {
                    ResourceId = resource.Value;
                }

                subCategory = LogAuditScheme.AuditScheme.AssetManagement.AssetCredential.Value;

                if (command.ActionStatus == ActionStatus.Reject)
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetCredential.Rejected.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetCredential.Rejected.Name;
                }
                else
                {
                    action = LogAuditScheme.AuditScheme.AssetManagement.AssetCredential.Approved.Value;
                    actionName = LogAuditScheme.AuditScheme.AssetManagement.AssetCredential.Approved.Name;
                }
            }


            if (asset.AssetTypeId != AssetTypeId)
            {
                return WorkflowResponseDTO.Failed("Invalid Attempt!");
            }


            var srqparams = new DynamicParameters();
            srqparams.Add("@AssetTypeId", AssetTypeId);
            srqparams.Add("@EntityId", entityId);
            srqparams.Add("@ResourceId", ResourceId);
            srqparams.Add("@WorkFlowType", command.WorkFlowType);
            var stepsRemain = new List<WorkflowStep>();
            WorkflowStep currentStep = null;

            if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval ||
                command.WorkFlowType == WorkFlowType.OwnerTransferApproval)
            {

                stepsRemain = (await _readRepository.GetLazyRepository<WorkflowStep>().Value.
                   GetListAsync($@" select 
                                    *
                                    from WorkflowSteps rws
                                    where 
                                    rws.WorkFlowType = @WorkFlowType and
                                    ISNULL(rws.ResourceId, 0) = @ResourceId and 
                                    rws.AssetTypeId = @AssetTypeId and
                                    rws.StepId not in
                                    (select  ws.StepId 
                                    from WorkflowEntities we
                                    inner join WorkflowSteps ws on ws.StepId = we.StepId
                                    where 
                                    we.EntityId = @EntityId and
                                    ws.WorkflowType = @WorkFlowType and
                                    ISNULL(ws.ResourceId, 0) = @ResourceId and 
                                    ws.AssetTypeId = @AssetTypeId)
                                    order by rws.StepOrder",
                   cancellationToken,
                   srqparams, null, "text")).ToList();


                int actionUserId = proxyUserId == 0 ?
                                     currentUserId :
                                     proxyUserId;

                currentStep = stepsRemain.FirstOrDefault(s =>
                     s.AuthorityType == AuthorityType.Individual &&
                     s.AuthorityId == actionUserId);

            }
            else
            {
                stepsRemain = (await _readRepository.GetLazyRepository<WorkflowStep>().Value.
                   GetListAsync($@" select 
                                                     *
                                                     from WorkflowSteps rws
                                                     where 
                                                     rws.WorkFlowType = @WorkFlowType and
                                                     ISNULL(rws.ResourceId, 0) = @ResourceId and 
                                                     rws.AssetTypeId = @AssetTypeId and
                                                     rws.StepOrder >
                                                     isnull(
                                                     (select Top(1) ws.StepOrder 
                                                     from WorkflowEntities we
                                                     inner join WorkflowSteps ws on ws.StepId = we.StepId
                                                     where 
                                                     we.EntityId = @EntityId and
                                                     ws.WorkflowType = @WorkFlowType and
                                                     ISNULL(ws.ResourceId, 0) = @ResourceId and 
                                                     ws.AssetTypeId = @AssetTypeId
                                                     order by rws.StepOrder desc),
                                                     0
                                                     )
                                                     order by rws.StepOrder",
                   cancellationToken,
                   srqparams, null, "text")).ToList();

                currentStep = stepsRemain.FirstOrDefault();
            }


           var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var nextStepExist = stepsRemain.Count() > 1;

            if (currentStep != null && !nextStepExist)
            {


                if (!(await checkAuthorization(asset, assetType, command, currentStep, 
                                            currentUserId, proxyUserId, cancellationToken)))
                {
                    return WorkflowResponseDTO.Failed("Unauthorized Attempt!");
                }


                var result = await processWorkflowStep(
                                  command, action, actionName, subCategory, currentStep,
                                  currentUserId, entityId, ResourceId,
                                  asset, assetType, dbTransaction,
                                  cancellationToken, true);

                return result != 0 ? WorkflowResponseDTO.Ok() : WorkflowResponseDTO.Failed("Unexpected Error!");

            }
            else if (currentStep != null && nextStepExist)
            {
                if (!(await checkAuthorization(asset, assetType, command, currentStep,
                            currentUserId, proxyUserId, cancellationToken)))
                {
                    return WorkflowResponseDTO.Failed("Unauthorized Attempt!");
                }

                var result = await processWorkflowStep(
                                  command, action, actionName, subCategory, currentStep,
                                  currentUserId, entityId, ResourceId,
                                  asset, assetType, dbTransaction,
                                  cancellationToken, false);


                return result != 0 ? WorkflowResponseDTO.Ok() : WorkflowResponseDTO.Failed("Unexpected Error!");

            }
            else
            {
                return WorkflowResponseDTO.Failed("Invalid Attempt!");
            }




        }



    }
}
