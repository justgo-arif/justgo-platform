using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.OwnerLeaseApprovalMetadata
{
    public class GetOwnerLeaseApprovalMetadataQueryHandler : IRequestHandler<GetOwnerLeaseApprovalMetadataQuery, List<OwnerLeaseApprovalMetadataDTO>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private IUtilityService _utilityService;

        public GetOwnerLeaseApprovalMetadataQueryHandler(
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<List<OwnerLeaseApprovalMetadataDTO>> Handle(GetOwnerLeaseApprovalMetadataQuery request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            string sql = @" declare @assetLeaseId int = (select AssetLeaseId from AssetLeases 
                                                                   where RecordGuid = @recordGuid);

                            with
                            Roles as (
	                            select Id from 
	                            AbacRoles 
                                where Name in(
                                'System Admin',
                                'Asset Super Admin',
                                'Asset Admin',
                                'Asset Manager'
                                )
                            ),
                            UserHierarchyLinks as (
                                select h.[HierarchyId] [HierarchyId] from 
                                HierarchyLinks hl
                                Inner join Hierarchies h  on  h.[Id] = hl.[HierarchyId]
	                            Left join AbacUserRoles abcr  on  abcr.UserId = hl.UserId and abcr.OrganizationId = h.EntityId
	                            where hl.[UserId] = @CurrentUserId and 
                                (abcr.RoleId in (select r.Id from Roles r) or
                                    Exists(
		                            select * from GroupMembers 
		                            where GroupId in ( 25,1)
		                            And UserId = @CurrentUserId
		                            )
                                )
                            ),
                            UserClubs as (

                                select distinct h.EntityId ClubId from 
                                UserHierarchyLinks hl
                                Inner join Hierarchies h  on  h.[HierarchyId].IsDescendantOf(hl.[HierarchyId]) = 1
                            ),
                            FamilyMembers as (
	                            Select u.UserId UserId
	                            FROM [User] u
	                            inner join Family_Links fm on fm.Entityid = u.MemberDocId
	                            Where fm.Docid in (
	                               Select DocId from  Family_Links fl where fl.Entityid = 
	                               (Select MemberDocId from [User] where userid = @CurrentUserId))
                            ) 
                            select distinct 
                            ws.StepId,
                            u.UserId UserDocId,
                            CAST(u.UserSyncId AS nvarchar(100)) UserId,
                            concat(u.FirstName, ' ', u.LastName) Fullname,
                            u.EmailAddress,
                            u.ProfilePicURL,
                            u.MemberDocId,
                            u.MemberId,
                            we.ActionStatus,
                            we.Remarks
                            from WorkflowSteps ws
                            left join WorkflowEntities we on we.StepId = ws.StepId
                            inner join [user] u on u.[Userid] = ws.AuthorityId and ws.AuthorityType = 1
                            left join HierarchyLinks hl on hl.UserId = u.Userid
                            left join Hierarchies h on h.Id = hl.[HierarchyId]
                            left join UserClubs uc on uc.ClubId = h.Entityid
                            left join FamilyMembers fm on fm.UserId = u.Userid
                            where ws.WorkFlowType = 7 and ws.ResourceId = @assetLeaseId and
                            (fm.UserId is not null or uc.ClubId is not null)
                            ";

            DynamicParameters queryParameters = new DynamicParameters();
            queryParameters.Add("@recordGuid", request.LeaseId);
            queryParameters.Add("@currentUserId", currentUserId);
            var result = (await _readRepository
                                .GetLazyRepository<OwnerLeaseApprovalMetadataDTO>()
                                .Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList(); 

            return result;
        }
    }
}
