using MapsterMapper;
using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses;
using JustGo.AssetManagement.Application.Features.FilterHelper;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using JustGo.AssetManagement.Application.Features.Common.Helpers;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetActionAssets
{

    public class GetActionAssetsHandler : IRequestHandler<GetActionAssetsQuery, PagedResult<ActionRequiredItemDTO>>
    {
        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper  _mapper;
        private readonly IUtilityService _utilityService;
        public GetActionAssetsHandler(
            IMediator mediator,
            IMapper mapper,
            IReadRepositoryFactory readRepository,
            IUtilityService utilityService) 
        {
            _mediator = mediator;
            _mapper = mapper;
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<PagedResult<ActionRequiredItemDTO>> Handle(GetActionAssetsQuery request, CancellationToken cancellationToken)
        {

            var data = await FetchData(request, cancellationToken);
            return data;

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

        private async Task<PagedResult<ActionRequiredItemDTO>> FetchData(GetActionAssetsQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);


            var adminRoles = (await _readRepository.GetLazyRepository<SelectListItemDTO<int>>()
                              .Value.GetListAsync($@"select [Name] [Text], Id [Value] from AbacRoles 
                                                    where Name in(
                                                    'System Admin',
                                                    'Asset Super Admin',
                                                    'Asset Admin',
                                                    'Asset Manager'
                                                    )", cancellationToken, null, null, "text")).ToList();


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CurrentUserId", currentUserId);
            queryParameters.Add("@adminRoles", adminRoles.Select(r => r.Value).ToList());
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);

            string searchSql = $@" Where 
                               ((r.ReasonName = 'Lease Pending Owner Approval'
                                and weloa.EntityId is null
		                        and wsloa.AuthorityId = @currentuserid
		                        and wsloa.AuthorityType = 1
                                )
                                or
                                (r.ReasonName = 'Transfer Pending Owner Approval'
                                and wetroa.EntityId is null
		                        and wstroa.AuthorityId = @currentuserid
		                        and wstroa.AuthorityType = 1
                                )
                                or
                                (r.ReasonName in ('Lease Payment Pending', 'Lease Confirmation Pending')
                                and ud.UserId = aosp.OwnerId and aosp.EntityType = 2 and al.AssetLeaseId = aosp.EntityId
                                )
                                or
                                (r.ReasonName in ('Transfer Payment Pending', 'Transfer Confirmation Pending')
                                and ud.UserId = aosp.OwnerId and aosp.EntityType = 3 and atr.AssetOwnershipTransferId = aosp.EntityId
                                )
                                or
                                (r.ReasonName = 'License Awaiting Approval' and uc.DocId = pd.OwnerId
                                  and (
                                    (wsli.AuthorityType = 1 and wsli.AuthorityId = @CurrentUserId)
                                     or 
                                    (wsli.AuthorityType = 2 and wsli.AuthorityId = uc.HierarchyTypeId)
                                   )
                                   
                                 )
                                or
                                (r.ReasonName in ('Primary License Required', 'Additional License Required', 'Certificate Required', 'License Payment Pending') 
                                and ud.UserId = u.UserId 
                                )
                                or
                                (r.ReasonName = 'Certificate Awaiting Approval' 
                                 and uc.DocId = h.EntityId 
                                 and  (
                                        (wsc.AuthorityType = 1 and wsc.AuthorityId = @CurrentUserId)
                                         or 
                                        (wsc.AuthorityType = 2 and wsc.AuthorityId = uc.HierarchyTypeId)
                                      )
                                 
                                )
                                or
                                (r.ReasonName = 'License Awaiting Approval' and uc.DocId = h.EntityId 
                                 and JSON_VALUE(at.AssetTypeConfig, '$.ApproveLicenseByOwnerOnly') != 'true'
                                 and (
                                        (wsli.AuthorityType = 1 and wsli.AuthorityId = @CurrentUserId)
                                         or 
                                        (wsli.AuthorityType = 2 and wsli.AuthorityId = uc.HierarchyTypeId)
                                     )
                                  
                                )
                                or
                                (al.OwnerClubId = h.EntityId and r.ReasonName  = 'Lease Awaiting Approval' 
                                    and uc.DocId = h.EntityId and al.AssetLeaseId is not null
                                    and (
                                            (wsl.AuthorityType = 1 and wsl.AuthorityId = @CurrentUserId)
                                             or 
                                            (wsl.AuthorityType = 2 and wsl.AuthorityId = uc.HierarchyTypeId)
                                        )
                                    
                                )
                                or
                                (atr.OwnerClubId = h.EntityId and r.ReasonName  = 'Transfer Awaiting Approval' 
                                    and uc.DocId = h.EntityId and atr.AssetOwnershipTransferId is not null
                                    and (
                                            (wstr.AuthorityType = 1 and wstr.AuthorityId = @CurrentUserId)
                                             or 
                                            (wstr.AuthorityType = 2 and wstr.AuthorityId = uc.HierarchyTypeId)
                                        )
                                    
                                )
                               )  

                               and

                              ar.RecordStatus = {(int)RecordStatus.Active} and
                              ar.StatusId in ({(string.Join(',',AssetStatusHelper.getActionStatusIds()))}) and
                              ear.ReasonStatus = 1
                             ";


            if (request.SearchItems.Any()) {

                searchSql += " And ";
                searchSql += await GetSearchSql(queryParameters, request.SearchItems.ToList(), cancellationToken);

            }


            string dataSql1 = "";
            string dataSql2 = "";
            string countSql = "";

            //string sortingSql = " ORDER BY ar.AssetId Desc ";
            string sortingSql = " ORDER BY ear.RecordChangedDate Desc ";
            if (request.SortItems.Any())
            {

                sortingSql = " ORDER BY " + GetSortSql(request.SortItems);
            }




            string baseSql = $@"WITH 
                    UserHierarchyLinks as (
                        select h.[HierarchyId] [HierarchyId], h.HierarchyTypeId  HierarchyTypeId from 
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
                        select DISTINCT h.EntityId DocId, hl.HierarchyTypeId HierarchyTypeId from 
                        UserHierarchyLinks hl
                        Inner join Hierarchies h  on  h.[HierarchyId].IsDescendantOf(hl.[HierarchyId]) = 1
                    ),
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
                     )";

            

            dataSql1 = $@"{baseSql},
                        ActionList AS (
                          SELECT 
                            COUNT(*) OVER() AS TotalRows,
                            ear.EntityActionReasonId,
					        r.ReasonName Reason,
						    ear.ActionDescription [Description],
                            ar.RecordGuid AssetRegisterId, 
                            ar.AssetTypeId, 
                            ar.AssetId, 
                            ar.AssetReference, 
                            ar.AssetName,
                            al.RecordGuid AssetLeaseId,
                            atr.RecordGuid AssetTransferId,
                            atli.LicenseType LicenseType
                          FROM EntityActionREason ear
					      Inner join ActionReason r on r.ActionReasonId = ear.ActionReasonId
					      LEFT join AssetRegisters rar on rar.AssetId = ear.ActionEntityId and ActionEntityType = 1
					      LEFT join AssetLeases al on al.AssetLeaseId = ear.ActionEntityId and ActionEntityType = 2
					      LEFT join AssetLicenses ali on ali.AssetLicenseId = ear.ActionEntityId and ActionEntityType = 3
                          LEFT join AssetOwnershipTransfers atr on atr.AssetOwnershipTransferId = ear.ActionEntityId and ActionEntityType = 4
                          LEFT JOIN Products_Default pd on pd.DocId = ali.ProductId 
                          LEFT JOIN Products_Links pl on pl.DocId = pd.DocId 
                          LEFT join AssetCredentials acr on acr.AssetId = rar.AssetId
					      Left Join AssetOwnerships aosp on aosp.OwnerType != 3 And
                                                      (aosp.AssetId = rar.AssetId or 
					                                   aosp.AssetId = al.AssetId or
												       aosp.AssetId = ali.AssetId or
												       aosp.AssetId = atr.AssetId) 
					      Inner join AssetRegisters ar on ar.AssetId = rar.AssetId or 
					                                   ar.AssetId = al.AssetId or
												       ar.AssetId = ali.AssetId or
                                                       ar.AssetId = atr.AssetId 
                          Inner Join AssetOwners ao on ao.AssetId = ar.AssetId
					      Inner join AssetTypes at on at.AssetTypeId = ar.AssetTypeId
                          LEFT JOIN  AssetTypesLicenseLink atli on atli.AssetTypeId = ar.AssetTypeId 
                                                               and atli.LicenseDocId = pl.EntityId
					      INNER join [User] u on u.Userid = aosp.OwnerId and aosp.OwnerType = 2
                          INNER JOIN HierarchyLinks hl on hl.UserId = u.UserId
						  INNER JOIN Hierarchies h on h.Id = hl.[HierarchyId]
                          LEFT join WorkflowSteps wsli on wsli.ResourceId = pl.EntityId and wsli.WorkFlowType = 5 and wsli.AssetTypeId = ar.AssetTypeId
                          LEFT join WorkflowSteps wsc on wsc.ResourceId = acr.CredentialmasterDocId and wsc.WorkFlowType = 6  and wsc.AssetTypeId = ar.AssetTypeId                        
                          LEFT join WorkflowSteps wsl on  wsl.WorkFlowType = 4 and wsl.AssetTypeId = ar.AssetTypeId
                          LEFT join WorkflowSteps wstr on  wstr.WorkFlowType = 3 and wstr.AssetTypeId = ar.AssetTypeId
                          LEFT join WorkflowSteps wsloa on  wsloa.WorkFlowType = 7 and 
                                                            wsloa.ResourceId = al.AssetLeaseId and
                                                            wsloa.AuthorityType = 1 and 
                                                            wsloa.AuthorityId = @CurrentUserId
                          LEFT join WorkflowEntities weloa on weloa.StepId = wsloa.StepId 
                          LEFT join WorkflowSteps wstroa on  wstroa.WorkFlowType = 8 and 
                                                             wstroa.ResourceId = atr.AssetOwnershipTransferId and
                                                             wstroa.AuthorityType = 1 and 
                                                             wstroa.AuthorityId = @CurrentUserId
                          LEFT join WorkflowEntities wetroa on wetroa.StepId = wstroa.StepId                           
                          LEFT JOIN UsersData ud on ud.UserId = aosp.OwnerId and aosp.OwnerType = 2
                          LEFT JOIN UserClubs uc on uc.DocId = h.EntityId or uc.DocId = pd.OwnerId
                            {searchSql} 
					        group by	
                            ear.EntityActionReasonId,
					        r.ReasonName,
					        ear.ActionDescription,
                            ear.RecordChangedDate,
					        ar.RecordGuid, 
					        ar.AssetTypeId, 
					        ar.AssetId, 
					        ar.AssetReference, 
					        ar.AssetName,
                            al.RecordGuid,
                            atr.RecordGuid,
                            atli.LicenseType
                            {sortingSql}
	                        OFFSET (@PageNumber - 1) * @PageSize ROWS
	                        FETCH NEXT @PageSize ROWS ONLY
                        )
                        Select * from ActionList";

            var rawDatas1 = (await _readRepository.GetLazyRepository<ActionRequiredRawItemDTO>().Value.GetListAsync(dataSql1, cancellationToken, queryParameters, null, "text")).ToList();

            queryParameters.Add("@RawAssetIds", rawDatas1.Select(r => r.AssetId).ToList());

            dataSql2 = $@"With 
                        ActionList as (
                          Select a.AssetId from AssetRegisters a Where a.AssetId in @RawAssetIds
                        ),
                        AssetOwnerObjs as (
                            SELECT Distinct
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
                            Inner Join ActionList a on a.AssetId = ao.AssetId
                            Left Join Clubs_Default cd on cd.DocId = ao.OwnerId and ao.OwnerTypeId = 1
                            Left Join Document cdd on cdd.DocId = cd.DocId
                            Left Join [User] u on u.Userid = ao.OwnerId and ao.OwnerTypeId = 2
                        ),
                        RawAssetOwners as (
                            Select obj.AssetId, STRING_AGG(obj.Obj, '||') Owners
                            from AssetOwnerObjs obj
                            GROUP BY obj.AssetId
                        ),
                        ImageObjects as (
                            Select Distinct a.AssetId, 
                            CAST((
                                    SELECT 
                                        ai.AssetId AS AssetId,
                                        ai.AssetImageId AS AssetImageId,
			                            ai.RecordGuid AS ImageId,
			                            ai.AssetImage AS AssetImage,
			                            ai.IsPrimary AS IsPrimary
                                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                                ) AS NVARCHAR(MAX)) ImageObj
                            from ActionList a
                            INNER JOIN [dbo].[AssetImages] ai on ai.AssetId = a.AssetId
                        ),
                        Images as (
                            Select io.AssetId, STRING_AGG(io.ImageObj, '||') Images
                            from ImageObjects io
                            GROUP BY io.AssetId
                        )
                        Select 
                        a.*, rao.Owners, i.Images 
                        FROM ActionList a 
                        INNER JOIN RawAssetOwners rao on rao.AssetId = a.AssetId  
                        LEFT JOIN Images i on i.AssetId = a.AssetId";

            var rawDatas2 = (await _readRepository.GetLazyRepository<ActionRequiredRawItemDTO>().Value.GetListAsync(dataSql2, cancellationToken, queryParameters, null, "text")).ToList();

            var listData = (from dt1 in rawDatas1
                            join dt2 in rawDatas2 on dt1.AssetId equals dt2.AssetId
                            select new ActionRequiredRawItemDTO
                            {
                                // From dt2
                                AssetId = dt2.AssetId,
                                Owners = dt2.Owners,
                                Images = dt2.Images,



                                // From dt1
                                LicenseType = dt1.LicenseType,
                                AssetRegisterId = dt1.AssetRegisterId,
                                AssetLeaseId = dt1.AssetLeaseId,
                                AssetTransferId = dt1.AssetTransferId,
                                AssetReference = dt1.AssetReference,
                                AssetName = dt1.AssetName,
                                Reason = dt1.Reason,
                                Description = dt1.Description,
                            }).ToList();



            var result = new PagedResult<ActionRequiredItemDTO>()
            {
                Items = new List<ActionRequiredItemDTO>(),
                TotalCount  = rawDatas1.Count() > 0 ? rawDatas1[0].TotalRows : 0
            };

            for (int i = 0; i < listData.Count; i++)
            {
                result.Items.Add(_mapper.Map<ActionRequiredItemDTO>(listData[i]));

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

            }

             return result;
        }

    }
}
