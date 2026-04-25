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


namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetMyLeases
{

    public class GetMyLeasesHandler : IRequestHandler<GetMyLeasesQuery, PagedResult<LeaseListItemDTO>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper  _mapper;
        private readonly IUtilityService _utilityService;
        public GetMyLeasesHandler(
            IMediator mediator,
            IReadRepositoryFactory readRepository,
            IMapper mapper,
            IUtilityService utilityService) 
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _mapper = mapper;
            _utilityService = utilityService;
        }

        public async Task<PagedResult<LeaseListItemDTO>> Handle(GetMyLeasesQuery request, CancellationToken cancellationToken)
        {

            var rawData = await FetchRawData(request, cancellationToken);
            return GetResultFromRawData(rawData);

        }

        private PagedResult<LeaseListItemDTO> GetResultFromRawData(PagedResult<LeaseDTOWithRawData> rawData)
        {
            var result = new PagedResult<LeaseListItemDTO>()
            {
                TotalCount = rawData.TotalCount,
            };

            var listData = rawData.Items;

            for (int i = 0; i < listData.Count; i++)
            {
                result.Items.Add(_mapper.Map<LeaseListItemDTO>(listData[i]));

                result.Items[i].CounterParty =
                    listData[i].Owners != null ?
                        listData[i].Owners.Split("||").Select(r =>
                    JsonConvert.DeserializeObject<AssetOwnerViewDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList() : [];

                if (result.Items[i].CounterParty.Any())
                {
                    result.Items[i].CounterParty[0].IsPrimary = true;
                }

                result.Items[i].AssetImages =
                    listData[i].Images != null ?
                        listData[i].Images.Split("||").Select(r =>
                    JsonConvert.DeserializeObject<AssetImageDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList() : [];


            }

            return result;
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
                    "AssetID", "AssetName", "CounterpartyName", "CounterpartyMemberID"

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
      
        private async Task<PagedResult<LeaseDTOWithRawData>> FetchRawData(GetMyLeasesQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CurrentUserId", currentUserId);
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);

            string searchSql = $@" Where 
                    ar.RecordStatus = {(int)RecordStatus.Active} ";

            if (request.SearchItems.Any()) {

                searchSql += " And ";
                searchSql += await GetSearchSql(queryParameters, request.SearchItems, cancellationToken);
            }

            string dataSql1 = "";
            string dataSql2 = "";
            string countSql = "";

            string sortingSql = " ORDER BY ar.AssetId desc  ";

            if (request.SortItems.Any())
            {

                sortingSql = " ORDER BY " + GetSortSql(request.SortItems);
            }


            dataSql1 = $@"WITH
                        UsersData as (
                            Select DISTINCT u.UserId UserId
                            FROM [User] u
						    left join Family_Links fm on fm.Entityid = u.MemberDocId
                            Where 
                            (u.UserId = @CurrentUserId
                                or
                                fm.Docid in (Select DocId  
                                    from  Family_Links fl where fl.Entityid = (
                                    Select MemberDocId from [User]
                                    where userid = @CurrentUserId)))
                        ),
                        Assets as (
                            SELECT
                              ROW_NUMBER() OVER ({sortingSql}) AS RowIndex,
                              al.RecordGuid AssetLeaseId,
                              al.AssetLeaseId LeaseId,
                              ar.RecordGuid AssetRegisterId, 
                              ar.[AssetId],
                              ar.[AssetName],
                              ar.[AssetReference],
                              lst.Name LeaseStatus,
                              al.LeaseStartDate,
                              al.LeaseEndDate
                            FROM 
                              [dbo].[AssetRegisters] ar 
                              LEFT Join AssetOwners ao on ao.AssetId = ar.AssetId
                              Inner JOIN AssetLeases al on al.AssetId = ar.AssetId 
                              Inner JOIN AssetOwnerships aosp on aosp.AssetId = al.AssetId and aosp.OwnerType = 2 and aosp.OwnerType = 2 and
                                                                (aosp.EntityType = 1 or (aosp.EntityType = 2 and aosp.EntityId = al.AssetLeaseId ))  
                              Inner JOIN [User] u on u.UserId = ao.OwnerId and ao.OwnerTypeId = 2 
                              INNER JOIN UsersData ud on  ud.UserId = aosp.OwnerId
							  LEFT JOIN AssetOwnerships cposp on cposp.AssetId = al.AssetId and cposp.OwnerType = 2 and
                                                                (cposp.EntityType = 1 or (cposp.EntityType = 2 and cposp.EntityId = al.AssetLeaseId ))                                         
							  Inner JOIN [User] cp on cp.UserId = cposp.OwnerId  and aosp.EntityType != cposp.EntityType
                              INNER JOIN [dbo].[AssetStatus] lst on lst.AssetStatusId = al.StatusId 
                              and lst.Type = 2
                             {searchSql} 
                             group by 
                              al.RecordGuid,
                              al.AssetLeaseId,
                              ar.RecordGuid, 
                              ar.[AssetId],
                              ar.[AssetName],
                              ar.[AssetReference],
                              lst.Name,
                              al.LeaseStartDate,
                              al.LeaseEndDate
                             {sortingSql} 
                             OFFSET (@PageNumber -1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY
                        )
                        Select * from Assets";

            var rawDatas1 = (await _readRepository.GetLazyRepository<LeaseDTOWithRawData>().Value.GetListAsync(dataSql1, cancellationToken, queryParameters, null, "text")).ToList();

            queryParameters.Add("@RawLeaseIds", rawDatas1.Select(r => r.LeaseId).ToList());


            dataSql2 = $@"WITH
                        UsersData as (
                            Select DISTINCT u.UserId UserId
                            FROM [User] u
						    left join Family_Links fm on fm.Entityid = u.MemberDocId
                            Where 
                            (u.UserId = @CurrentUserId
                                or
                                fm.Docid in (Select DocId  
                                    from  Family_Links fl where fl.Entityid = (
                                    Select MemberDocId from [User]
                                    where userid = @CurrentUserId)))
                        ), 
                        Assets as (
                          Select
                            al.RecordGuid AssetLeaseId, 
                            al.AssetId,
                            Min(case 
                                when ale.EntityId is not null then 1 
                                else 0
                            end) LeasedIn
                            from AssetLeases al 
                            inner JOIN AssetOwnerships ao on ao.AssetId = al.AssetId and ao.OwnerType != 3
                            Inner JOIN UsersData ud on  ud.UserId = ao.OwnerId
                            left JOIN AssetOwnerships ale on ale.EntityId = al.AssetLeaseId and ale.OwnerType != 3 and ale.EntityType = 2 and ud.UserId = ale.OwnerId
                            Where al.AssetLeaseId in @RawLeaseIds
                            group by
                            al.RecordGuid, 
                            al.AssetId
                        ),
                        AssetOwnerObjs as (
                            SELECT Distinct
                            a.AssetLeaseId AssetLeaseId,
                            a.AssetId AssetId,
                            CAST((select
                                    ao.OwnerTypeId OwnerTypeId,
                                    Case When ao.OwnerTypeId = 0 Then ''
                                         When ao.OwnerTypeId = 1 Then cd.ClubId
	                                     Else  u.MemberId
                                    End OwnerReferenceId,
                                    Case When ao.OwnerTypeId = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.NAME')
                                         When ao.OwnerTypeId = 1 Then cd.ClubName
	                                     Else CONCAT(u.FirstName, ' ', u.LastName)
                                    End OwnerName,
                                    Case When ao.OwnerTypeId = 0 Then null
                                         When ao.OwnerTypeId = 1 Then CAST(cdd.SyncGuid as nvarchar(255))
	                                     Else CAST(u.UserSyncId as nvarchar(255))
                                    End OwnerId,
                                    Case When ao.OwnerTypeId = 0 Then (select value from SystemSettings where itemkey like 'ORGANISATION.LOGO')
                                         When ao.OwnerTypeId = 1 Then cd.[Location]
	                                     Else u.ProfilePicURL
                                    End ProfileImage,
                                    Case When ao.OwnerTypeId = 0 Then 0
                                         When ao.OwnerTypeId = 1 Then cd.DocId
	                                     Else u.Userid
                                    End OwnerDocId
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                ) AS NVARCHAR(MAX)) Obj
                            from 
                            AssetOwners ao
                            Inner Join Assets a on a.AssetId = ao.AssetId 
                            Left Join Clubs_Default cd on cd.DocId = ao.OwnerId and ao.OwnerTypeId = 1
                            Left Join Document cdd on cdd.DocId = cd.DocId
                            Left Join [User] u on u.Userid = ao.OwnerId and ao.OwnerTypeId = 2
                            where a.LeasedIn = 1

                            union all

                            SELECT Distinct
                            a.AssetLeaseId AssetLeaseId,
                            a.AssetId AssetId,
                            CAST((select
                                    2 OwnerTypeId,
                                    u.MemberId OwnerReferenceId,
                                    CONCAT(u.FirstName, ' ', u.LastName) OwnerName,
                                    CAST(u.UserSyncId as nvarchar(255)) OwnerId,
                                    u.ProfilePicURL ProfileImage,
                                    u.Userid OwnerDocId
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                ) AS NVARCHAR(MAX)) Obj
                            from 
                            AssetLeases al
                            Inner JOIN AssetOwnerships ao on ao.EntityId = al.AssetLeaseId and ao.OwnerType != 3 and ao.EntityType = 2
                            Inner Join Assets a on a.AssetLeaseId = al.RecordGuid  
                            Inner Join [User] u on u.Userid = ao.OwnerId 
                            where a.LeasedIn = 0
                        ),
                        DistinctAssetOwnerObjs as (
                              Select Distinct obj.AssetLeaseId, obj.AssetId, obj.Obj from AssetOwnerObjs obj
                         ),
                        RawAssetOwners as (
                            Select obj.AssetLeaseId, obj.AssetId, STRING_AGG(obj.Obj, '||') Owners
                            from DistinctAssetOwnerObjs obj
                            GROUP BY obj.AssetLeaseId, obj.AssetId
                        ),
                        ImageObjects as (
                            Select Distinct a.AssetLeaseId, a.AssetId, 
                            CAST((
                                    SELECT 
                                        ai.AssetId AS AssetId,
                                        ai.AssetImageId AS AssetImageId,
			                            ai.RecordGuid AS ImageId,
			                            ai.AssetImage AS AssetImage,
			                            ai.IsPrimary AS IsPrimary
                                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                ) AS NVARCHAR(MAX)) ImageObj
                            from Assets a
                            INNER JOIN [dbo].[AssetImages] ai on ai.AssetId = a.AssetId
                        ),
                        Images as (
                            Select io.AssetLeaseId, io.AssetId, STRING_AGG(io.ImageObj, '||') Images
                            from ImageObjects io
                            GROUP BY io.AssetLeaseId, io.AssetId
                        )
                        Select distinct a.*, rao.Owners, i.Images
                        FROM Assets a
                        LEFT JOIN RawAssetOwners rao on rao.AssetLeaseId = a.AssetLeaseId  and rao.AssetId = a.AssetId 
                        LEFT JOIN Images i on i.AssetLeaseId = a.AssetLeaseId and i.AssetId = a.AssetId
                        ";


            var rawDatas2 = (await _readRepository.GetLazyRepository<LeaseDTOWithRawData>().Value.GetListAsync(dataSql2, cancellationToken, queryParameters, null, "text")).ToList();



            var rawDatas = (from dt1 in rawDatas1
                            join dt2 in rawDatas2 on  new { dt1.AssetId, dt1.AssetLeaseId } equals new { dt2.AssetId, dt2.AssetLeaseId }
                            orderby dt1.RowIndex
                            select new LeaseDTOWithRawData
                            {
                                // From dt2
                                AssetId = dt2.AssetId,
                                Owners = dt2.Owners,
                                Images = dt2.Images,
                                LeasedIn = dt2.LeasedIn,

                                // From dt1
                                AssetRegisterId = dt1.AssetRegisterId,
                                AssetReference = dt1.AssetReference,
                                AssetLeaseId = dt1.AssetLeaseId,
                                AssetName = dt1.AssetName,
                                LeaseStartDate = dt1.LeaseStartDate,
                                LeaseEndDate = dt1.LeaseEndDate,
                                LeaseStatus = dt1.LeaseStatus,
                            }).ToList();

            countSql = $@"WITH
                                UsersData as (
                                    Select DISTINCT u.UserId UserId
                                    FROM [User] u
						            left join Family_Links fm on fm.Entityid = u.MemberDocId
                                    Where 
                                    (u.UserId = @CurrentUserId
                                        or
                                        fm.Docid in (Select DocId  
                                            from  Family_Links fl where fl.Entityid = (
                                            Select MemberDocId from [User]
                                            where userid = @CurrentUserId)))
                                )
                                SELECT Count(DISTINCT al.AssetLeaseId) TotalRowCount
                                FROM [dbo].[AssetRegisters] ar 
                                  Inner Join AssetOwners ao on ao.AssetId = ar.AssetId
                                  Inner JOIN AssetLeases al on al.AssetId = ar.AssetId 
                                  Inner JOIN AssetOwnerships aosp on aosp.AssetId = al.AssetId and aosp.OwnerType = 2 and aosp.OwnerType = 2 and
                                                                (aosp.EntityType = 1 or (aosp.EntityType = 2 and aosp.EntityId = al.AssetLeaseId ))  
                                  Inner JOIN [User] u on u.UserId = ao.OwnerId and ao.OwnerTypeId = 2 
                                  INNER JOIN UsersData ud on  ud.UserId = aosp.OwnerId
							      Inner JOIN AssetOwnerships cposp on cposp.AssetId = al.AssetId and cposp.OwnerType = 2 and
                                                                (cposp.EntityType = 1 or (cposp.EntityType = 2 and cposp.EntityId = al.AssetLeaseId )) 
							      Inner JOIN [User] cp on cp.UserId = cposp.OwnerId  and aosp.EntityType != cposp.EntityType
                                  INNER JOIN [dbo].[AssetStatus] lst on lst.AssetStatusId = al.StatusId 
                                  and lst.Type = 2 
                                  {searchSql}";



            var countData = await _readRepository.GetLazyRepository<CountDTO>().Value.GetAsync(countSql, cancellationToken, queryParameters, null, "text");



            return new PagedResult<LeaseDTOWithRawData>()
            {
                Items = rawDatas,
                TotalCount = countData.TotalRowCount
            };
        }

    }
}
