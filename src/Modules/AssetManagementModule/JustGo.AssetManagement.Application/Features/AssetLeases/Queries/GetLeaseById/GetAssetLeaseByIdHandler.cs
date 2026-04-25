using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseById
{
    public class GetAssetLeaseByIdHandler : IRequestHandler<GetAssetLeaseByIdQuery, AssetLeaseDetailDTO>
    {
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepository;

        public GetAssetLeaseByIdHandler(
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }
        public async Task<AssetLeaseDetailDTO> Handle(GetAssetLeaseByIdQuery request, CancellationToken cancellationToken)
        {

            string baseSql = @"With 
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
                            ),
                            UserFamilyData as (
                            select @CurrentUserId UserId, 1 Self
                            union
                            Select  m.UserId UserId, 0 Self
                            FROM [User] m
                            inner join Family_Links fm on fm.Entityid = m.MemberDocId
                            inner join Family_Links fl on fl.DocId = fm.DocId
                            inner join [User] u on  u.MemberDocId = fl.Entityid  
                            Where u.userid = @CurrentUserId
                            ),
                            UserFamily as (
                            Select  m.UserId UserId, Max(m.Self) Self
                            FROM UserFamilyData m
                            group by m.UserId
                            )";

            string dataSql = baseSql +
                             @"select Distinct 
                                 AL.RecordGuid As AssetLeaseId
                                ,ATT.AttachmentPath AS AttachmentName
                                ,ATT.RecordGuid AS LeaseAttachmentId
                                ,LeaseStartDate
                                ,LeaseEndDate
                                ,ISNULL(DateRangeType,0) as DateRangeType
                                ,AR.AssetName
                                ,AR.AssetReference
                                ,AI.AssetImage
                                ,AI.RecordGuid AS AssetImageId
                                ,AR.RecordGuid AS AssetRegisterId
                                ,S.Name as LeaseStatus
                                ,AL.AssetLeaseId LeaseDocCode
                                ,Al.OwnerClubId
                                ,case 
                                    when ars.OrganizationId is not null then 1
                                    when exists(select * from DefaultAdminRoles) then 1
                                    else 0
                                 end IsAdmin
                                ,
                                  case 
                                    when lars.OrganizationId is not null then 1
                                    when exists(select * from DefaultAdminRoles) then 1
                                    else 0
                                  end IsLeaseeAdmin
                                ,
                                case 
                                    when oars.OrganizationId is not null then 1
                                    when exists(select * from DefaultAdminRoles) then 1
                                    else 0
                                end IsOwnerAdmin
                                ,
                                case 
                                    when [of].UserId is not null then 1
                                    else 0
                                end IsOwnerFamily
                                ,
                                case 
                                    when lf.UserId is not null then 1
                                    else 0
                                end IsLeaseeFamily
                                ,
                                case 
                                    when [of].Self = 1 then 1
                                    else 0
                                end IsOwner
                                ,
                                case 
                                    when lf.Self = 1 then 1
                                    else 0
                                end IsLeasee
                                from AssetLeases AL
                                INNER JOIN AssetOwnerships AO ON 
                                  AO.AssetId = AL.AssetId and AO.EntityType = 1 AND AO.OwnerType = 2
                                LEFT JOIN ClubMemberroles CMR on CMR.UserId = AO.OwnerId
                                INNER JOIN AssetOwnerships LO ON 
                                  LO.EntityId = AL.AssetLeaseId and LO.EntityType = 2 AND LO.OwnerType = 2
                                LEFT JOIN ClubMemberroles LOCMR on LOCMR.UserId = LO.OwnerId
                                INNER JOIN AssetStatus S on S.AssetStatusId = AL.StatusId
                                LEFT JOIN AssetLeaseAttachments ATT on ATT.AssetLeaseId = AL.AssetLeaseId	
                                INNER JOIN AssetRegisters AR on AR.AssetId = AL.AssetId	
                                LEFT JOIN AssetImages AI on AI.AssetId = AR.AssetId AND AI.IsPrimary = 1
                                LEFT JOIN AdminRoleStates ars on ars.OrganizationId = AL.OwnerClubId
                                LEFT JOIN AdminRoleStates lars on lars.OrganizationId = LOCMR.ClubDocId
                                LEFT JOIN AdminRoleStates oars on oars.OrganizationId = CMR.ClubDocId
                                LEFT JOIN UserFamily [of] on [of].UserId = AO.OwnerId
                                LEFT JOIN UserFamily lf on lf.UserId = LO.OwnerId
                                where AL.RecordGuid = @RecordGuid
                                ";

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetLeaseId);
            queryParameters.Add("@CurrentUserId", currentUserId);

            var flatData = (await _readRepository.GetLazyRepository<AssetLeaseResultDTO>() .Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();

            var lease = flatData
               .GroupBy(f => new
               {
                   f.AssetLeaseId,
                   f.LeaseStartDate,
                   f.LeaseEndDate,
                   f.DateRangeType,
                   f.AssetName,
                   f.AssetReference,
                   f.AssetImage,
                   f.AssetImageId,
                   f.AssetRegisterId,
                   f.LeaseStatus,
                   f.LeaseDocCode,
                   f.OwnerClubId,
                   f.IsAdmin
               })
               .Select(g => new AssetLeaseDetailDTO
               {
                   AssetLeaseId = g.Key.AssetLeaseId,
                   LeaseStartDate = g.Key.LeaseStartDate,
                   LeaseEndDate = g.Key.LeaseEndDate,
                   DateRangeType = g.Key.DateRangeType,
                   AssetName = g.Key.AssetName,
                   AssetRegisterId = g.Key.AssetRegisterId,
                   AssetReference = g.Key.AssetReference,
                   AssetImage = g.Key.AssetImage,
                   AssetImageId = g.Key.AssetImageId,
                   LeaseStatus = g.Key.LeaseStatus,
                   LeaseDocCode = g.Key.LeaseDocCode,
                   OwnerClubId = g.Key.OwnerClubId,
                   IsAdmin = g.Key.IsAdmin,
                   IsLeaseeAdmin = g.Any( r => r.IsLeaseeAdmin),
                   IsOwnerAdmin = g.Any(r => r.IsOwnerAdmin),
                   IsLeaseeFamily = g.Any(r => r.IsLeaseeFamily),
                   IsOwnerFamily = g.Any(r => r.IsOwnerFamily),
                   IsLeasee = g.Any(r => r.IsLeasee),
                   IsOwner = g.Any(r => r.IsOwner),
                   LeaseAttachment = g
                       .Where(static x => !string.IsNullOrEmpty(x.LeaseAttachmentId))
                       .Select(x => new AssetLeaseAttachmentDTO
                       {
                           LeaseAttachmentId = x.LeaseAttachmentId,
                           AttachmentName = x.AttachmentName
                       })
                       .ToList()
               }).FirstOrDefault();

            lease.LeaseOwners = await GetLeaseOwners(request.AssetLeaseId, cancellationToken);

            string sql = @" declare @assetLeaseId int = (select AssetLeaseId from AssetLeases 
                                                                   where RecordGuid = @recordGuid);

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
                            where ws.WorkFlowType = 7 and ws.ResourceId = @assetLeaseId";

            lease.OwnerApprovals = (await _readRepository
                                .GetLazyRepository<OwnerLeaseApprovalMetadataDTO>()
                                .Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            
            lease.CanOwnerApprove = lease.OwnerApprovals.Any(o => 
                                       lease.LeaseStatus == "Pending Owner Approval" &&
                                       o.UserDocId == currentUserId &&
                                       o.ActionStatus == null
                                     );

            return lease;
        }

        private async Task<List<AssetLeaseOwnerDetailViewDTO>> GetLeaseOwners(string RecordGuid, CancellationToken cancellationToken)
        {
            var sql = $@"select 
                        ao.OwnerType OwnerTypeId,
                        Case When ao.OwnerType = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.NAME')
                             When ao.OwnerType = 1 Then cd.ClubName
	                         Else CONCAT(u.FirstName, ' ', u.LastName)
                        End OwnerName,
                        Case When ao.OwnerType = 0 Then null
                             When ao.OwnerType = 1 Then CAST(cdd.SyncGuid as nvarchar(255))
	                         Else CAST(u.UserSyncId as nvarchar(255))
                        End OwnerId,
                        Case When ao.OwnerType = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.LOGO')
                             When ao.OwnerType = 1 Then cd.[Location]
	                         Else u.ProfilePicURL
                        End ProfileImage,
                        Case When ao.OwnerType = 0 Then ''
                                When ao.OwnerType = 1 Then cd.ClubId
	                            Else  u.MemberId
                        End OwnerReferenceId,
                        Case When ao.OwnerType = 0 Then 0
                                When ao.OwnerType = 1 Then cd.DocId
	                            Else u.Userid
                        End OwnerDocId,
                        Case When ao.OwnerType = 0 Then ''
                                When ao.OwnerType = 1 Then cd.ClubemailAddress
	                            Else ISNULL(u.EmailAddress,'')
                        End Email,
                        Case When ao.OwnerType = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.NAME')
                             When ao.OwnerType = 1 Then cd.ClubName
	                         Else u.FirstName
                        End FirstName,
                        Case When ao.OwnerType = 0 Then ''
                             When ao.OwnerType = 1 Then ''
	                         Else u.LastName
                        End LastName
                        from 
                        AssetOwnerships ao
                        Inner Join AssetLeases al on al.AssetLeaseId = ao.EntityId and ao.EntityType = 2
                        Left Join Clubs_Default cd on cd.DocId = ao.OwnerId and ao.OwnerType = 1
                        Left Join Document cdd on cdd.DocId = cd.DocId
                        Left Join [User] u on u.Userid = ao.OwnerId and ao.OwnerType = 2
                        WHERE al.RecordGuid = @RecordGuid ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", RecordGuid);

            var result = (await _readRepository.GetLazyRepository<AssetLeaseOwnerDetailViewDTO>()
                     .Value.GetListAsync(sql, cancellationToken, queryParameters,
                         null, "text")).ToList();

            if (result.Any())
            {
                result[0].IsPrimary = true;
            }

            return result;
        }
    }
}
