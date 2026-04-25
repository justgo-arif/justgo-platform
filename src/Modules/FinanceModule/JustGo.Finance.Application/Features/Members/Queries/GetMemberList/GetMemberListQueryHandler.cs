using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Filters;
using JustGo.Finance.Application.DTOs.MemberDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Members.Queries.GetMemberList
{
    public class GetMemberListQueryHandler : IRequestHandler<GetMemberListQuery, PaginatedResponse<UserDocumentInfo>>
    {
        private readonly LazyService<IReadRepository<UserDocumentInfo>> _readRepository;
        private readonly IMediator _mediator;

        public GetMemberListQueryHandler(LazyService<IReadRepository<UserDocumentInfo>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaginatedResponse<UserDocumentInfo>> Handle(GetMemberListQuery request, CancellationToken cancellationToken)
        {
            string CommonCondition = "";
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            if (request.UserSyncIds?.Count == 1 && request.UserSyncIds[0] == "string") request.UserSyncIds.Clear();
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var queryParameters = new DynamicParameters();
            queryParameters.Add("OwningEntityId", ownerId);
            if (request.UserSyncIds != null && request.UserSyncIds.Count > 0)
            {
                request.PageNo = 1;
                request.PageSize = request.UserSyncIds.Count;
                CommonCondition += " AND U.UserSyncId IN @UserSyncIds ";
                queryParameters.Add("UserSyncIds", request.UserSyncIds);
            }
            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                CommonCondition += @$" AND ( CONCAT( u.FirstName,' ',u.LastName) LIKE @SearchText  
                                OR u.MemberId LIKE @SearchText 
                                OR U.EmailAddress LIKE @SearchText ) ";
                queryParameters.Add("SearchText", $"%{request.SearchText}%");
            }


            var sql = @$" Declare @excludeStates nvarchar(max) = (select IsNull((select value from SystemSettings where ItemKey = 'ORGANISATION.EXCLUDEMEMBERSTATEFROMEMAILINGE'),'') value)
                        declare @ProcessId int
                        Set @ProcessId  = (Select  Process.ProcessId from  Process  where  Process.[Name] = 'Members')  

                        IF(@OwningEntityId>0)
                        BEGIN

                        SELECT Distinct  U.MemberDocId AS DocId, 
                             ISNULL(U.MemberId,'') AS MID, 
                            ISNULL(U.FirstName,'') as FirstName, 
                            ISNULL(U.LastName,'') as LastName, 
                            ISNULL(U.UserId,0) as UserId, 
                            CAST(U.UserSyncId AS nvarchar(36)) as UserSyncId, 
                            ISNULL(U.ProfilePicURL,'') AS ImageSrc,
                            ISNULL(U.EmailAddress,'') as EmailAddress
                        FROM ClubMembers_Links CML 
                        INNER JOIN [User] U ON 
                        U.MemberDocId = CML.Entityid and
                        CML.EntityParentId = 1
                        INNER JOIN ClubMembers_Links MEMBER_CLUBS ON 
                        MEMBER_CLUBS.DocId = CML.DocId and
                        MEMBER_CLUBS.EntityParentId = 2
                        INNER JOIN ClubMembers_Default ON  ClubMembers_Default.DocId = MEMBER_CLUBS.DocId
                        and IsNull(ClubMembers_Default.Ishidden, 0) = 0
                        INNER JOIN Clubs_Default ON Clubs_Default.DocId = MEMBER_CLUBS.Entityid 
                        INNER JOIN ProcessInfo p ON u.MemberDocId = p.PrimaryDocId
                        INNER JOIN [State] S ON S.StateId = p.CurrentStateId
                        WHERE S.ProcessId =@ProcessId AND S.[Name] NOT IN (select * from splitlargestring(@excludeStates,',')) 
                        AND clubs_default.docid =@OwningEntityId 
                        AND S.[Name]<>'Merged'
                        {CommonCondition}
                          ORDER BY 3 , 4
                        OFFSET ((@PageNo-1)*@PageSize) rows fetch next  @PageSize rows only
                        END
                        ELSE
                        BEGIN

                        SELECT 
                            U.MemberDocId AS DocId, 
                            ISNULL(U.MemberId,'') AS MID, 
                            ISNULL(U.FirstName,'') as FirstName, 
                            ISNULL(U.LastName,'') as LastName, 
                            ISNULL(U.UserId,0) as UserId, 
                            CAST(U.UserSyncId AS nvarchar(36)) as UserSyncId, 
                            ISNULL(U.ProfilePicURL,'') AS ImageSrc,
                            ISNULL(U.EmailAddress,'') as EmailAddress
                        FROM [User] U
                        INNER JOIN ProcessInfo p ON u.MemberDocId = p.PrimaryDocId
                        INNER JOIN [State] S ON S.StateId = p.CurrentStateId
                        WHERE S.ProcessId =@ProcessId AND S.[Name] NOT IN (select * from splitlargestring(@excludeStates,',')) 
                        AND S.[Name]<>'Merged'
                        {CommonCondition}
                        ORDER BY U.FirstName, U.LastName 
                        OFFSET ((@PageNo-1)*@PageSize) rows fetch next  @PageSize rows only
                        END ";
            if ((request?.TotalCount ?? 0) <= 0)
            {

                var sqlCount = @$" Declare @excludeStates nvarchar(max) = (select IsNull((select value from SystemSettings where ItemKey = 'ORGANISATION.EXCLUDEMEMBERSTATEFROMEMAILINGE'),'') value)
                                declare @ProcessId int
                                Set @ProcessId  = (Select  Process.ProcessId from  Process  where  Process.[Name] = 'Members')  

                                IF(@OwningEntityId>0)
                                BEGIN

                                    SELECT Distinct  Count(U.MemberDocId) as TotalCount
                                    FROM ClubMembers_Links CML 
                                    INNER JOIN [User] U ON 
                                    U.MemberDocId = CML.Entityid and
                                    CML.EntityParentId = 1
                                    INNER JOIN ClubMembers_Links MEMBER_CLUBS ON 
                                    MEMBER_CLUBS.DocId = CML.DocId and
                                    MEMBER_CLUBS.EntityParentId = 2
                                    INNER JOIN ClubMembers_Default ON  ClubMembers_Default.DocId = MEMBER_CLUBS.DocId
                                    and IsNull(ClubMembers_Default.Ishidden, 0) = 0
                                    INNER JOIN Clubs_Default ON Clubs_Default.DocId = MEMBER_CLUBS.Entityid 
                                    INNER JOIN ProcessInfo p ON u.MemberDocId = p.PrimaryDocId
                                    INNER JOIN [State] S ON S.StateId = p.CurrentStateId
                                    WHERE S.ProcessId =@ProcessId AND S.[Name] NOT IN (select * from splitlargestring(@excludeStates,',')) 
                                    AND clubs_default.docid =@OwningEntityId 
                                    AND S.[Name]<>'Merged'
                                    {CommonCondition}
                                END
                                ELSE
                                BEGIN
                                    SELECT 
                                       Count(*) as TotalCount
                                    FROM [User] U
                                    INNER JOIN ProcessInfo p ON u.MemberDocId = p.PrimaryDocId
                                    INNER JOIN [State] S ON S.StateId = p.CurrentStateId
                                    WHERE S.ProcessId =@ProcessId AND S.[Name] NOT IN (select * from splitlargestring(@excludeStates,','))
                                    AND S.[Name]<>'Merged'
                                    {CommonCondition}
                                END ";

                var totalcountobj = await _readRepository.Value.GetSingleAsync(sqlCount, cancellationToken, queryParameters, null, "text");
                if (request is not null)
                {
                    request.TotalCount = totalcountobj != null ? Convert.ToInt32(totalcountobj) : 0;
                }
            }


            var data = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            return new PaginatedResponse<UserDocumentInfo>(data, request!.PageNo, request.PageSize, request.TotalCount);
        }
    }
}
