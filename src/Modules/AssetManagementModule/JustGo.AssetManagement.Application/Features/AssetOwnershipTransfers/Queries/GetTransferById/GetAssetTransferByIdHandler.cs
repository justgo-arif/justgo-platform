using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetTransfers;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.GetTransferById
{
    public class GetAssetTransferByIdHandler : IRequestHandler<GetAssetTransferByIdQuery, AssetTransferDetailDTO>
    {
        private readonly IUtilityService _utilityService;
        private readonly IReadRepositoryFactory _readRepository;

        public GetAssetTransferByIdHandler(
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }
        public async Task<AssetTransferDetailDTO> Handle(GetAssetTransferByIdQuery request, CancellationToken cancellationToken)
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
                            )";

            string dataSql = baseSql +
                             @"select Distinct
                                 CONCAT(u.FirstName, ' ', u.LastName) InitiatedByFullName
                                ,CAST(u.UserSyncId as nvarchar(255)) InitiatedByUserId
                                ,u.ProfilePicURL InitiatedByProfileImage
                                ,u.MemberId InitiatedByReferenceId
                                ,u.UserId InitiatedByDocId
                                ,ISNULL(u.EmailAddress,'') InitiatedByEmail
                                ,u.MemberId InitiatedByReferenceId
                                ,ATR.RecordGuid As AssetTransferId
                                ,ATT.AttachmentPath AS AttachmentName
                                ,ATT.RecordGuid AS TransferAttachmentId
                                ,ATR.TransferDate
                                ,AR.AssetName
                                ,AR.AssetReference
                                ,AI.AssetImage
                                ,AI.RecordGuid AS AssetImageId
                                ,AR.RecordGuid AS AssetRegisterId
                                ,S.Name as TransferStatus
                                ,ATR.AssetOwnershipTransferId TransferDocCode
                                ,ATR.AssetId AssetDocCode
                                ,case 
                                    when ars.OrganizationId is not null then 1
                                    when exists(select * from DefaultAdminRoles) then 1
                                    else 0
                                    end IsAdmin
                                ,
                                    case 
                                    when oars.OrganizationId is not null then 1
                                    when exists(select * from DefaultAdminRoles) then 1
                                    else 0
                                    end IsTransferToAdmin
                                from AssetOwnershipTransfers ATR
                                INNER JOIN [User] u on u.Userid = ATR.CreatedBy 
                                left JOIN AssetTransferOwners AO ON AO.AssetOwnershipTransferId = ATR.AssetOwnershipTransferId AND 
	                                 AO.OwnerType = 2
                                LEFT JOIN ClubMemberroles CMR on CMR.UserId = AO.OwnerId
                                INNER JOIN AssetStatus S on S.AssetStatusId = ATR.TransferStatusId
                                LEFT JOIN AssetTransferAttachments ATT on ATT.AssetOwnershipTransferId = ATR.AssetOwnershipTransferId	
                                INNER JOIN AssetRegisters AR on AR.AssetId = ATR.AssetId	
                                LEFT JOIN AssetImages AI on AI.AssetId = AR.AssetId AND AI.IsPrimary = 1
                                LEFT JOIN AdminRoleStates ars on ars.OrganizationId = ATR.OwnerClubId
                                LEFT JOIN AdminRoleStates oars on oars.OrganizationId = CMR.ClubDocId
                                where ATR.RecordGuid = @RecordGuid
                                ";

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.AssetTransferId);
            queryParameters.Add("@CurrentUserId", currentUserId);

            var flatData = (await _readRepository.GetLazyRepository<AssetTransferResultDTO>() .Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();

            var Transfer = flatData
               .GroupBy(f => new
               {
                   f.AssetTransferId,
                   f.TransferDate,
                   f.AssetName,
                   f.AssetReference,
                   f.AssetImage,
                   f.AssetImageId,
                   f.AssetRegisterId,
                   f.TransferStatus,
                   f.TransferDocCode,
                   f.AssetDocCode,
                   f.IsAdmin,
                   f.InitiatedByUserId,
                   f.InitiatedByEmail,
                   f.InitiatedByFullName,
                   f.InitiatedByProfileImage,
                   f.InitiatedByDocId,
                   f.InitiatedByReferenceId,

               })
               .Select(g => new AssetTransferDetailDTO
               {
                   AssetTransferId = g.Key.AssetTransferId,
                   TransferDate = g.Key.TransferDate,
                   AssetName = g.Key.AssetName,
                   AssetRegisterId = g.Key.AssetRegisterId,
                   AssetReference = g.Key.AssetReference,
                   AssetImage = g.Key.AssetImage,
                   AssetImageId = g.Key.AssetImageId,
                   TransferStatus = g.Key.TransferStatus,
                   TransferDocCode = g.Key.TransferDocCode,
                   AssetDocCode = g.Key.AssetDocCode,   
                   IsAdmin = g.Key.IsAdmin,
                   InitiatedByUserId = g.Key.InitiatedByUserId,
                   InitiatedByEmail = g.Key.InitiatedByEmail,
                   InitiatedByFullName = g.Key.InitiatedByFullName,
                   InitiatedByProfileImage = g.Key.InitiatedByProfileImage,
                   InitiatedByDocId = g.Key.InitiatedByDocId,
                   InitiatedByReferenceId = g.Key.InitiatedByReferenceId,
                   IsTransferToAdmin = g.Any( r => r.IsTransferToAdmin),
                   TransferAttachment = g
                       .Where(static x => !string.IsNullOrEmpty(x.TransferAttachmentId))
                       .Select(x => new AssetTransferAttachmentDTO
                       {
                           TransferAttachmentId = x.TransferAttachmentId,
                           AttachmentName = x.AttachmentName
                       })
                       .ToList()
               }).FirstOrDefault();

            Transfer.TransferOwners = await GetTransferOwners(request.AssetTransferId, cancellationToken);


            return Transfer;
        }

        private async Task<List<AssetTransferOwnerDetailViewDTO>> GetTransferOwners(string RecordGuid, CancellationToken cancellationToken)
        {
            var sql = $@"select 
                        atr.RecordGuid  AssetTransferId,
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
                        AssetTransferOwners ao
                        Inner Join AssetOwnershipTransfers atr on atr.AssetOwnershipTransferId = ao.AssetOwnershipTransferId
                        Left Join Clubs_Default cd on cd.DocId = ao.OwnerId and ao.OwnerType = 1
                        Left Join Document cdd on cdd.DocId = cd.DocId
                        Left Join [User] u on u.Userid = ao.OwnerId and ao.OwnerType = 2
                        WHERE atr.RecordGuid = @RecordGuid ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", RecordGuid);

            var result = (await _readRepository.GetLazyRepository<AssetTransferOwnerDetailViewDTO>()
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
