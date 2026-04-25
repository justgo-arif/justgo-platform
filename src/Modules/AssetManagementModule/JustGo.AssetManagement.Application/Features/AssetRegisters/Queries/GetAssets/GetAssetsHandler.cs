using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLeaseStatuses;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Application.Features.FilterHelper;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;



namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetAssets
{

    public class GetAssetsHandler : IRequestHandler<GetAssetsQuery, PagedResult<AssetListItemDTO>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper  _mapper;
        private readonly IUtilityService _utilityService;
        public GetAssetsHandler(
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

        public async Task<PagedResult<AssetListItemDTO>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
        {

            var rawData = await FetchRawData(request, cancellationToken);
            return GetResultFromRawData(rawData);

        }

        private PagedResult<AssetListItemDTO> GetResultFromRawData(PagedResult<AssetDTOWithRawData> rawData)
        {
            var result = new PagedResult<AssetListItemDTO>()
            {
                TotalCount = rawData.TotalCount,
            };

            var listData = rawData.Items;

            for (int i = 0; i < listData.Count; i++)
            {
                result.Items.Add(_mapper.Map<AssetListItemDTO>(listData[i]));

                result.Items[i].AssetOwners =
                    listData[i].Owners != null ?
                        listData[i].Owners.Split("||").Select(r =>
                    JsonConvert.DeserializeObject<AssetOwnerViewDTO>(r,
                    new JsonSerializerSettings
                    {
                        Error = (s, e) => e.ErrorContext.Handled = true
                    })
                ).Where(r => r is not null).ToList() : [];

                if (result.Items[i].AssetOwners.Any())
                {
                    result.Items[i].AssetOwners[0].IsPrimary = true;
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

                result.Items[i].AssetTags =
                    listData[i].Tags != null ?
                    listData[i].Tags.Split(',').Select(r => r).ToList() : [];

                if (!String.IsNullOrEmpty(listData[i].PrimaryLicenseInfo))
                {
                    result.Items[i].PrimaryLicenses = listData[i].PrimaryLicenseInfo.Split("||").Select(r =>
                            JsonConvert.DeserializeObject<AssetLicenseDTO>(r,
                            new JsonSerializerSettings
                            {
                                Error = (s, e) => e.ErrorContext.Handled = true
                            })
                        ).Where(r => r is not null).ToList();

                   
                }

                if (!String.IsNullOrEmpty(listData[i].AdditionalLicenseInfo))
                {
                    result.Items[i].AdditionalLicenses = listData[i].AdditionalLicenseInfo.Split("||").Select(r =>
                            JsonConvert.DeserializeObject<AssetLicenseDTO>(r,
                            new JsonSerializerSettings
                            {
                                Error = (s, e) => e.ErrorContext.Handled = true
                            })
                        ).Where(r => r is not null).ToList();

                }

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
                    "AssetID", "AssetName", "OwnerName", "MemberID"

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
                whereBuilder.Append((await searchConditionResolver.GetSQLConditions(queryParameters, searchItems)));
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

                whereBuilder.Append((await searchConditionResolver.GetSQLConditions(queryParameters, fieldWiseQuery)));
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

        private async Task<PagedResult<AssetDTOWithRawData>> FetchRawData(GetAssetsQuery request, CancellationToken cancellationToken)
        {

            var adminRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                              .Value.GetListAsync($@"select [Name] [Text], Id [Value] from AbacRoles 
                                                    where Name in(
                                                    'System Admin',
                                                    'Asset Super Admin',
                                                    'Asset Admin',
                                                    'Asset Manager'
                                                    )", cancellationToken, null, null, "text")).ToList();

            var NGBadminRoles = adminRoles.Where(r => r.Text == "System Admin" || r.Text == "Asset Super Admin");

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CurrentUserId", currentUserId);
            queryParameters.Add("@adminRoles", NGBadminRoles.Select(r => r.Value).ToList());

            var isNGBAdmin = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                  .Value.GetListAsync($@"select '' [Text], UserId [Value] from AbacUserRoles 
                                                    where RoleId in @adminRoles and
                                                          UserId = @CurrentUserId", cancellationToken, 
                                                    queryParameters, null, "text")).Any();

            var showAllAssets = request.SkipHierarchyAssetsMode || isNGBAdmin;


            int activeLicenseStatusId = await _mediator.Send(new GetLicenseStatusIdQuery()
            {
                Status = LicenseStatusType.Active,
            });

            int activeCredentialStatusId = await _mediator.Send(new GetCredentialStatusIdQuery()
            {
                Status = CredentialStatusType.Active,
            });

            int activeLeaseStatusId = await _mediator.Send(new GetLeaseStatusIdQuery()
            {
                Status = LeaseStatusType.Active,
            });

            queryParameters = new DynamicParameters();
            queryParameters.Add("@CurrentUserId", currentUserId);
            queryParameters.Add("@activeLeaseStatusId", activeLeaseStatusId);
            queryParameters.Add("@activeLicenseStatusId", activeLicenseStatusId);
            queryParameters.Add("@activeCredentialStatusId", activeCredentialStatusId);
            queryParameters.Add("@adminRoles", adminRoles.Select(r => r.Value).ToList());
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);

            string searchSql = $@" Where ar.RecordStatus = {(int)RecordStatus.Active} ";


            if (request.SearchItems.Any()) {

                searchSql += " And ";
                searchSql += await GetSearchSql(queryParameters, request.SearchItems, cancellationToken);
            }

            string dataSql1 = "";
            string countSql = "";

            string sortingSql = " ORDER BY ar.AssetId Desc ";

            if (request.SortItems.Any())
            {

                sortingSql = " ORDER BY " + GetSortSql(request.SortItems);
            }

            var adfJoins = await AdditionalFieldQueryResolver.MakeJoinsForDynamicFormData(
                            _readRepository, request.SearchItems, "ar.AssetId", cancellationToken);



            dataSql1 = $@"WITH
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

                            select distinct h.EntityId DocId from 
                            {(!showAllAssets ? "UserHierarchyLinks" : "Hierarchies")} hl
                            Inner join Hierarchies h  on  h.[HierarchyId].IsDescendantOf(hl.[HierarchyId]) = 1
                        ),
                        Assets as (
                            SELECT
                                COUNT(*) OVER() AS TotalRows,
                                ROW_NUMBER() OVER ({sortingSql}) AS RowIndex,
                                Ar.AssetId
                                FROM [dbo].[AssetRegisters] ar " +

                                (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "User" || r.ColumnName == "All") ?
                              $@" {(showAllAssets ? " Left " : " INNER ")} JOIN AssetOwners ao on  ao.AssetId = ar.AssetId " 
                                : "") +

                                (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetLeases" or "AssetStatus") ?
                              @" LEFT JOIN AssetLeases al on  al.AssetId = ar.AssetId
                                 LEFT JOIN AssetStatus lst on  lst.AssetStatusId = al.StatusId and lst.Type = 2 "
                                : " ") +

                               $@"{(showAllAssets ? " Left " : " INNER ")} Join  AssetOwnerships aosp on aosp.AssetId = ar.AssetId and aosp.OwnerType != 3 and aosp.EntityType <= 2
                                {(showAllAssets ? " Left " : " INNER ")} JOIN [User] u ON u.UserId = aosp.OwnerId " + 
                              
                                (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).NameInQuery.Contains("ppl") && !r.Operator.Contains("not")) ?
                             @" INNER JOIN AssetLicenses pali on pali.AssetId = ar.AssetId and pali.StatusId = @activeLicenseStatusId and pali.LicenseType = 1
                                INNER JOIN Products_Links ppl on ppl.DocId = pali.ProductId "
                               : request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).NameInQuery.Contains("ppl") && r.Operator.Contains("not")) ?
                             @" LEFT JOIN AssetLicenses pali on pali.AssetId = ar.AssetId and pali.StatusId = @activeLicenseStatusId and pali.LicenseType = 1
                                LEFT JOIN Products_Links ppl on ppl.DocId = pali.ProductId "
                               : "")+

                             (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).NameInQuery.Contains("opl") && !r.Operator.Contains("not")) ?
                             @" INNER JOIN AssetLicenses oali on oali.AssetId = ar.AssetId and oali.StatusId = @activeLicenseStatusId and oali.LicenseType = 2
                                INNER JOIN Products_Links opl on opl.DocId = oali.ProductId "
                               : request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).NameInQuery.Contains("opl") && r.Operator.Contains("not")) ?
                             @" LEFT JOIN AssetLicenses oali on oali.AssetId = ar.AssetId and oali.StatusId = @activeLicenseStatusId and oali.LicenseType = 2
                                LEFT JOIN Products_Links opl on opl.DocId = oali.ProductId "
                               : "") +

                               (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetCredentials") ?
                              @" INNER JOIN AssetCredentials acr on acr.AssetId = ar.AssetId and acr.StatusId = @activeCredentialStatusId "
                                : "")+ 

                              $@"{(showAllAssets ? " Left " : " INNER ")} JOIN HierarchyLinks hl on hl.UserId = aosp.OwnerId
								{(showAllAssets ? " Left " : " INNER ")} JOIN Hierarchies h on h.Id = hl.[HierarchyId]
                                {(showAllAssets ? " Left " : " INNER ")} JOIN UserClubs uc on uc.DocId = h.Entityid " +

                              (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetCategories") ||
                               request.SortItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetCategories" ) ?
                              @" LEFT JOIN [dbo].[AssetCategories] ac on ac.AssetCategoryId = ar.AssetCategoryId "
                               : "") +

                              " INNER JOIN [dbo].[AssetStatus] ast on ast.AssetStatusId = ar.StatusId and ast.Type = 1 " +
                               

                             (request.SearchItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetTypesTag") ?
                             @" INNER JOIN [dbo].[AssetTagLink] atl on atl.AssetId = ar.AssetId
                                INNER JOIN [dbo].[AssetTypesTag] att on att.TagId = atl.TagId "
                                : "") +

                              $@" {adfJoins}
                                {searchSql} 
                                group by 
                                ar.AssetId
                               {(request.SortItems.Any(r => r.ColumnName is "AssetName") ?
                                 ",ar.AssetName" : "")} 
                               {(request.SortItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetCategories") ?
                                 ",ac.[Name]" : "")}
                               {(request.SortItems.Any(r => ColumnNameResolver.GetColumnName(r.ColumnName).SourceEntity is "AssetStatus") ?
                                 ",ast.[Name]" : "")} 
                                {sortingSql}
                                OFFSET (@PageNumber - 1) * @PageSize ROWS
                                FETCH NEXT @PageSize ROWS ONLY
                        )
                        Select * from Assets
                        ";



            var rawDatas1 = (await _readRepository.GetLazyRepository<AssetDTOWithRawData>().Value.GetListAsync(dataSql1, cancellationToken, queryParameters, null, "text")).ToList();

            queryParameters.Add("@RawAssetIds", rawDatas1.Select(r => r.AssetId).ToList());

            string dataSql2 = $@"With 
                        Assets as (
                          Select 
                                a.RecordGuid AssetRegisterId,
                                a.*,
                                ac.Name Category,
                                ac.RecordGuid CategoryId,
                                ast.Name AssetStatus
                           from AssetRegisters a 
                                LEFT JOIN [dbo].[AssetCategories] ac on ac.AssetCategoryId = a.AssetCategoryId
                                INNER JOIN [dbo].[AssetStatus] ast on ast.AssetStatusId = a.StatusId and ast.Type = 1
                            Where a.AssetId in @RawAssetIds
                        ),
                        AssetLeaseStatuses as (
                           select 
                                a.AssetId,
                                case when Max(isnull(aal.StatusId, 0)) != 0 then 1 
                                     else 0 
                                end Leased
                              from
                              Assets a
                              inner join AssetLeases aal on  aal.AssetId = a.AssetId And aal.StatusId = @activeLeaseStatusId 
                              group by a.AssetId
                        ),
                        AssetOwnerObjs as (
                            SELECT 
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
                        ),
                        RawAssetOwners as (
                            Select obj.AssetId, STRING_AGG(obj.Obj, '||') Owners
                            from AssetOwnerObjs obj
                            GROUP BY obj.AssetId
                        ),
                        Tags as (
                            Select a.AssetId, STRING_AGG(att.Name, ',') Tags from 
                            Assets a
                            INNER JOIN [dbo].[AssetTagLink] atl on atl.AssetId = a.AssetId
                            INNER JOIN [dbo].[AssetTypesTag] att on att.TagId = atl.TagId
                            GROUP BY a.AssetId
                        ),
                        ImageObjects as (
                            Select a.AssetId, 
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
                            Select io.AssetId, STRING_AGG(io.ImageObj, '||') Images
                            from ImageObjects io
                            GROUP BY io.AssetId
                        ),
                        PrimaryLicenses as (
                                SELECT 
                                    ar.AssetId AssetId,
                                        CAST((
                                            SELECT 
                                                ast.Name AS LicenseStatus,
                                                al.EndDate AS EndDate,
                                                pd.[Name] AS [Name]
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                        ) AS NVARCHAR(MAX)) AS LicenseInfo
                                FROM 
                                    Products_Default pd
                                    INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
                                    INNER JOIN AssetLicenses al ON al.ProductId = pd.DocId and al.StatusId = @activeLicenseStatusId
                                    INNER JOIN AssetStatus ast ON ast.AssetStatusId = al.StatusId 
                                    INNER JOIN AssetRegisters ar ON ar.AssetId = al.AssetId
                                    INNER JOIN Assets a ON a.AssetId = ar.AssetId                                   
                                    INNER JOIN AssetTypes at ON at.AssetTypeId = ar.AssetTypeId
                                    INNER JOIN AssetTypesLicenseLink atl 
                                        ON atl.AssetTypeId = ar.AssetTypeId AND atl.LicenseDocId = ll.DocId
                                WHERE 
                                    atl.LicenseType = 1
                        ),
                        PrimaryLicenseData as (
                           Select 
                             pl.AssetId,
                             STRING_AGG(LicenseInfo, '||') LicenseInfo
                            from PrimaryLicenses pl
                            Group By pl.AssetId
                        ),
                        AdditionalLicenses as (
                                SELECT 
                                    ar.AssetId AssetId,
                                        CAST((
                                            SELECT 
                                                ast.Name AS LicenseStatus,
                                                al.EndDate AS EndDate,
                                                pd.[Name] AS [Name]
                                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                        ) AS NVARCHAR(MAX)) AS LicenseInfo
                                FROM 
                                    Products_Default pd
                                    INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
                                    INNER JOIN AssetLicenses al ON al.ProductId = pd.DocId and al.StatusId = @activeLicenseStatusId
                                    INNER JOIN AssetStatus ast ON ast.AssetStatusId = al.StatusId 
                                    INNER JOIN AssetRegisters ar ON ar.AssetId = al.AssetId
                                    INNER JOIN Assets a ON a.AssetId = ar.AssetId                                   
                                    INNER JOIN AssetTypes at ON at.AssetTypeId = ar.AssetTypeId
                                    INNER JOIN AssetTypesLicenseLink atl 
                                        ON atl.AssetTypeId = ar.AssetTypeId AND atl.LicenseDocId = ll.DocId
                                WHERE 
                                    atl.LicenseType = 2
                        ),
                        AdditionalLicenseData as (
                           Select 
                             al.AssetId,
                             STRING_AGG(LicenseInfo, '||') LicenseInfo
                            from AdditionalLicenses al
                            Group By al.AssetId
                        )
                        Select a.*, rao.Owners, i.Images, atg.Tags, 
                        pld.LicenseInfo PrimaryLicenseInfo,
                        ald.LicenseInfo AdditionalLicenseInfo,
                        isnull(als.Leased, 0) Leased
                        FROM Assets a
                        LEFT JOIN PrimaryLicenseData pld on pld.AssetId = a.AssetId 
                        LEFT JOIN AdditionalLicenseData ald on ald.AssetId = a.AssetId 
                        LEFT JOIN RawAssetOwners rao on rao.AssetId = a.AssetId  
                        LEFT JOIN Images i on i.AssetId = a.AssetId
                        LEFT JOIN Tags atg on atg.AssetId = a.AssetId
                        LEFT JOIN AssetLeaseStatuses als on als.AssetId = a.AssetId ";

            var rawDatas2 = (await _readRepository.GetLazyRepository<AssetDTOWithRawData>().Value.GetListAsync(dataSql2, cancellationToken, queryParameters, null, "text")).ToList();

            rawDatas2 = (from r1 in rawDatas1
                         join r2 in rawDatas2 on r1.AssetId equals r2.AssetId
                         orderby r1.RowIndex
                         select r2 ).ToList();
           


            return new PagedResult<AssetDTOWithRawData>()
            {
                Items = rawDatas2,
                TotalCount = rawDatas1.Count() > 0 ? rawDatas1[0].TotalRows : 0
            };
        }

    }
}
