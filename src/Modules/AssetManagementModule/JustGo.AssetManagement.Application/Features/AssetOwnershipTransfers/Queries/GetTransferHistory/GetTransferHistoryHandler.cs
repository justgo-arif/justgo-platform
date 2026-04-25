using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.FilterHelper;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using System.Text;

namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferHistory
{

    public class GetTransferHistoryHandler : IRequestHandler<GetTransferHistoryQuery, PagedResult<TransferHistoryItemDTO>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public GetTransferHistoryHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IMapper mapper,
            IUtilityService utilityService) 
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<PagedResult<TransferHistoryItemDTO>> Handle(GetTransferHistoryQuery request, CancellationToken cancellationToken)
        {

            return await FetchData(request, cancellationToken);

        }


        private async Task<string> GetSearchSql(DynamicParameters queryParameters, List<SearchSegmentDTO> SearchItems, CancellationToken cancellationToken)
        {
            var whereBuilder = new StringBuilder();

            var allFieldQuery = SearchItems.FirstOrDefault(r => r.ColumnName == "All");
            var fieldWiseQuery = SearchItems.Where(r => r.ColumnName != "All").ToList();

            bool hasPreviousCondition = false;

            SearchConditionResolver searchConditionResolver = new SearchConditionResolver(_mediator, _readRepository, cancellationToken);

            // Handle "All" fields search (multi-column OR logic)
            if (allFieldQuery != null)
            {
                var allFields = new List<string>()
                {
                    "UserName", "TransferStatus"

                };

                var searchItems = ColumnNameResolver.ColumnNameDictionary
                    .Values.Where(r => allFields.Contains(r.NameInView))
                    .Select(col => new SearchSegmentDTO
                    {
                        ColumnName = col.NameInView,
                        Operator = "contains",
                        Value = allFieldQuery.Value,
                        ConditionJoiner = "or"
                    })
                    .ToList();

                whereBuilder.Append("(");
                whereBuilder.Append(await searchConditionResolver.GetSQLConditions(queryParameters, searchItems));
                whereBuilder.Append(")");
                hasPreviousCondition = true;
            }

            // Handle field-specific filters
            if (fieldWiseQuery.Any())
            {
                if (hasPreviousCondition)
                {
                    whereBuilder.Append(" AND ");
                }

                whereBuilder.Append(await searchConditionResolver.GetSQLConditions(queryParameters, fieldWiseQuery));
            }

            return whereBuilder.ToString();
        }

        private string GetSortSql(List<SortItemDTO> SortItems)
        {
            var sortingSqlBuilder = new StringBuilder(" ");

            for (int i = 0; i < SortItems.Count; i++)
            {
                var item = SortItems[i];
                sortingSqlBuilder
                    .Append(
                        ColumnNameResolver.GetColumnName(item.ColumnName).NameInQuery
                    );

                if (item.OrderByDesceding)
                {
                    sortingSqlBuilder.Append(" desc");
                }

                if (i < SortItems.Count - 1)
                {
                    sortingSqlBuilder.Append(", ");
                }
            }

            return sortingSqlBuilder.ToString();
        }
      
        private async Task<PagedResult<TransferHistoryItemDTO>> FetchData(GetTransferHistoryQuery request, CancellationToken cancellationToken)
        {
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CurrentUserId", currentUserId);
            queryParameters.Add("@AssetRegisterId", request.AssetRegisterId);
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);

            string searchSql = $@" Where 
                    ar.RecordGuid = @AssetRegisterId And
                    ar.RecordStatus = {(int)RecordStatus.Active} ";

            if (request.SearchItems.Any()) {

                searchSql += " And ";
                searchSql += await GetSearchSql(queryParameters, request.SearchItems, cancellationToken);
            }

            string baseSql = "";
            string dataSql = "";
            string countSql = "";

            string sortingSql = " ORDER BY atr.TransferDate desc  ";

            if (request.SortItems.Any())
            {

                sortingSql = " ORDER BY " + GetSortSql(request.SortItems);
            }


            baseSql = @"With 
                          DefaultAdminRoles as (
                            Select top 1 1 IsAdmin 
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

            dataSql = baseSql + $@"SELECT  
                            atr.RecordGuid AssetTransferId,
                            ar.RecordGuid AssetRegisterId, 
                            tst.Name TransferStatus,
                            atr.TransferDate,
                            Max(case 
                                when ars.OrganizationId is not null then 1
                                when dar.IsAdmin is not null then 1
                                else 0
                            end) IsAdmin,
                            Max(
                             case 
                                when aoars.OrganizationId is not null then 1
                                when dar.IsAdmin is not null then 1
                                else 0
                             end) IsOwnersAdmin
                            FROM 
                            [dbo].[AssetRegisters] ar 
                            Inner JOIN AssetOwnershipTransfers atr on atr.AssetId = ar.AssetId 
                            LEFT JOIN AssetOwnerships ao on ao.AssetId = atr.AssetId and ao.EntityType = 1 and ao.OwnerType = 2
                            LEFT JOIN ClubMemberroles CMR on CMR.UserId = AO.OwnerId
                            LEFT JOIN AssetTransferOwners ato on ato.AssetOwnershipTransferId = atr.AssetOwnershipTransferId and ato.OwnerType = 2
                            LEFT JOIN [User] u on u.Userid = ato.OwnerId 
                            Inner JOIN [dbo].[AssetStatus] tst on tst.AssetStatusId = atr.TransferStatusId and tst.Type = 5
	                        left join AdminRoleStates ars on ars.OrganizationId = atr.OwnerClubId
                            left join AdminRoleStates aoars on aoars.OrganizationId = CMR.ClubDocId
                            LEFT JOIN DefaultAdminRoles dar ON dar.IsAdmin = 1
                            {searchSql} 
                            group by
                            atr.RecordGuid,
                            ar.RecordGuid, 
                            tst.Name,
                            atr.TransferDate
                            {sortingSql} 
                            OFFSET (@PageNumber -1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";

             countSql = baseSql + $@"SELECT  Count(Distinct atr.RecordGuid) TotalRowCount
                            FROM 
                            [dbo].[AssetRegisters] ar 
                            Inner JOIN AssetOwnershipTransfers atr on atr.AssetId = ar.AssetId 
                            LEFT JOIN AssetOwnerships ao on ao.AssetId = atr.AssetId and ao.EntityType = 1 and ao.OwnerType = 2
                            LEFT JOIN ClubMemberroles CMR on CMR.UserId = AO.OwnerId
                            LEFT JOIN AssetTransferOwners ato on ato.AssetOwnershipTransferId = atr.AssetOwnershipTransferId and ato.OwnerType = 2
                            LEFT JOIN [User] u on u.Userid = ato.OwnerId 
                            Inner JOIN [dbo].[AssetStatus] tst on tst.AssetStatusId = atr.TransferStatusId and tst.Type = 5
	                        left join AdminRoleStates ars on ars.OrganizationId = atr.OwnerClubId
                            left join AdminRoleStates aoars on aoars.OrganizationId = CMR.ClubDocId
                            LEFT JOIN DefaultAdminRoles dar ON dar.IsAdmin = 1
                              {searchSql}
                              ";


            var countData = await _readRepository.GetLazyRepository<CountDTO>().Value.GetAsync(countSql, cancellationToken, queryParameters, null, "text");

            var result = (await _readRepository.GetLazyRepository<TransferHistoryItemDTO>().Value.GetListAsync(dataSql, cancellationToken, queryParameters, null, "text")).ToList();

            var transferToOwners = await GetTransferOwners(result.Select(r => r.AssetTransferId).ToList(), true, cancellationToken);

            var transferToOwnersDict = transferToOwners
                    .GroupBy(lo => lo.AssetTransferId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

            var prevOwners = await GetTransferOwners(result.Select(r => r.AssetTransferId).ToList(), false, cancellationToken);

            var prevOwnersDict = prevOwners
                    .GroupBy(lo => lo.AssetTransferId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

            result = result.Select(r =>
            {
                r.TransferToOwners = transferToOwnersDict.ContainsKey(r.AssetTransferId) ?  transferToOwnersDict[r.AssetTransferId]: [];
                r.PreviousOwners = prevOwnersDict.ContainsKey(r.AssetTransferId) ?  prevOwnersDict[r.AssetTransferId] : [];
                return r;
            }).ToList();


            return new PagedResult<TransferHistoryItemDTO>()
            {
                Items = result,
                TotalCount = countData.TotalRowCount
            };
        }

        private async Task<List<AssetTransferOwnerGridViewDTO>> GetTransferOwners(
            List<string> RecordGuids, 
            bool TransferTo, 
            CancellationToken cancellationToken)
        {
                var sql = "";

                if (TransferTo)
                {

                   sql = $@"select 
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
                        WHERE atr.RecordGuid in @RecordGuids ";

            }
            else
            {
                sql = $@"select 
                        atr.RecordGuid  AssetTransferId,
                        1 OwnerTypeId,
                        CONCAT(u.FirstName, ' ', u.LastName) OwnerName,
                        CAST(u.UserSyncId as nvarchar(255)) OwnerId,
                        u.ProfilePicURL ProfileImage,
                        u.MemberId OwnerReferenceId,
                        u.Userid OwnerDocId,
                        ISNULL(u.EmailAddress,'') Email,
                        u.FirstName FirstName,
                        u.LastName LastName
                        from 
                        WorkflowSteps ws
                        Inner Join AssetOwnershipTransfers atr on atr.AssetOwnershipTransferId = ws.ResourceId and ws.AuthorityType = 1 and ws.WorkFlowType = 8
                        Left Join [User] u on u.Userid = ws.AuthorityId 
                        WHERE atr.RecordGuid in @RecordGuids ";
            }



            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuids", RecordGuids);

            var result = (await _readRepository.GetLazyRepository<AssetTransferOwnerGridViewDTO>()
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
