using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Enums;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MapsterMapper;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetMembersMetadata
{
    public class GetMembersMetadataHandler : IRequestHandler<GetMembersMetadataQuery, PagedResult<MemberDTO>>
    {

        private readonly IMediator _mediator;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IMapper _mapper;
        public GetMembersMetadataHandler(
            IReadRepositoryFactory readRepository, 
            IMediator mediator, 
            IUtilityService utilityService,
            IMapper mapper)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _utilityService = utilityService;
            _mapper = mapper;
        }

        public async Task<PagedResult<MemberDTO>> Handle(GetMembersMetadataQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            //      var clubDocIds = request.ClubIds.Any() ?
            //                         await _mediator.Send(
            //                               new GetIdByGuidQuery()
            //                               {
            //                                   Entity = AssetTables.Document,
            //                                   RecordGuids = request.ClubIds,
            //                               }                    
            //                         ): [];

            //      string baseSql = $@"With UserClubs AS (
            //                      SELECT DISTINCT abcr.OrganizationId ClubDocId
            //                      FROM [User]  u 
            //                      INNER JOIN AbacUserRoles abcr  on abcr.UserId = u.UserId
            //                      WHERE u.UserId = {currentUserId}
            //                      {(request.ClubIds.Any() ?
            //                       $@" and abcr.OrganizationId in @clubDocIds " : "")
            //                      }
            //                      )";


            //      string countSql = $@"{baseSql}
            //                  SELECT DISTINCT 
            //                  Count(DISTINCT u.MemberDocId) TotalRowCount
            //                  FROM UserClubs cd
            //INNER JOIN AbacUserRoles abcr  on abcr.OrganizationId = cd.ClubDocId 
            //                  INNER JOIN [User] u on u.UserId = abcr.UserId
            //                  Where 
            //                  Concat(u.FirstName, ' ', u.LastName) like @query or
            //                  u.MemberId like @query or
            //                  u.EmailAddress like @query";

            //      string sql = $@"{baseSql}
            //                  SELECT DISTINCT 
            //                  u.UserId Id,
            //                  u.MemberDocId MemberDocId,
            //                  u.MemberId MID,
            //                  u.FirstName FirstName,
            //                  u.LastName LastName,
            //                  cast(u.UserSyncId as nvarchar(255)) UserId,
            //                  u.EmailAddress EmailAddress,
            //                  u.Gender Gender,
            //                  u.ProfilePicURL [Image]
            //                  FROM UserClubs cd
            //INNER JOIN AbacUserRoles abcr  on abcr.OrganizationId = cd.ClubDocId 
            //                  INNER JOIN [User] u on u.UserId = abcr.UserId
            //                  Where 
            //                  Concat(u.FirstName, ' ', u.LastName) like @query or
            //                  u.MemberId like @query or
            //                  u.EmailAddress like @query
            //                  Order By u.FirstName, u.LastName
            //                  OFFSET (@PageNumber - 1) * @PageSize ROWS
            //                  FETCH NEXT @PageSize ROWS ONLY";

            string conditionSQL = "";
            if (!string.IsNullOrEmpty(request.Query))
            {
                conditionSQL = @" AND ( 
                        Concat(u.FirstName, ' ', u.LastName) like @query or
                        u.MemberId like @query or
                        u.EmailAddress like @query )
                        ";
            }


            string dataSQL = $@"DECLARE @NodeIds TABLE (HierarchyId hierarchyid);

                              IF EXISTS (
                                  SELECT 1
                                  FROM AbacUserRoles AUR
                                  INNER JOIN AbacRoles AR ON AR.Id = AUR.RoleId
                                  WHERE AUR.UserId = @UserId
                                    AND AR.Name IN ('Asset Super Admin', 'System Admin')
                              )
                              BEGIN
                                  INSERT INTO @NodeIds
                                  SELECT H.HierarchyId
                                  FROM Hierarchies H
                                  WHERE EntityName = (SELECT Value FROM SystemSettings WHERE Itemkey = 'organisation.name');
                              END
                              ELSE
                              BEGIN
                                  INSERT INTO @NodeIds
                                  SELECT H.HierarchyId
                                  FROM Hierarchies H
                                  INNER JOIN AbacUserRoles AR on AR.OrganizationId = H.EntityId
                                  Where AR.UserId = @UserId ANd AR.OrganizationId > 0
                              END
                              
                              ;WITH
                              CTE_TOTAL_MEMBER AS (
                                  SELECT
                                      HL.EntityId,
                                      ROW_NUMBER() OVER (ORDER BY U.MemberId DESC) AS RowNumber
                                  FROM dbo.Hierarchies H
                                  INNER JOIN dbo.HierarchyLinks HL ON H.Id = HL.HierarchyId
                                  INNER JOIN [User] U ON U.UserId = HL.UserId
                                  WHERE EXISTS (
                                      SELECT 1
                                      FROM @NodeIds nodes
                                      WHERE H.HierarchyId.IsDescendantOf(nodes.HierarchyId) = 1
                                  )
                                  AND ISNULL(HL.IsHidden, 0) = 0
                                  {conditionSQL}
                              ),
                              CTE_MEMBER_UNIQUE AS (
                                  SELECT EntityId, MIN(RowNumber) AS RowNumber
                                  FROM CTE_TOTAL_MEMBER
                                  GROUP BY EntityId
                              ),
                              CTE_MEMBER AS (
                                  SELECT
                                      EntityId,
                                      RowNumber,
                                      (SELECT COUNT(1) FROM CTE_MEMBER_UNIQUE) AS TotalRows
                                  FROM CTE_MEMBER_UNIQUE
                                  ORDER BY RowNumber
                                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY
                              ),
                              CTE_EXCLUDESTSTUS  AS (
                                  SELECT StateId FROM [State] WHERE Processid = 1 And Name IN (
                                  SELECT TRIM(value) AS StateName
                                     FROM STRING_SPLIT(
                                         (SELECT TOP 1 Value 
                                          FROM SystemSettings 
                                          WHERE ItemKey = 'ORGANISATION.EXCLUDEMEMBERSTATEFROMEMAILING'), 
                                         ','
                                     )
                                     WHERE TRIM(value) != '')
                              )
                              SELECT
                                  U.UserId AS Id,
                                  U.MemberDocId,
                                  U.MemberId AS MID,
                                  U.FirstName,
                                  U.LastName,
                                  CAST(U.UserSyncId AS NVARCHAR(255)) AS UserId,
                                  U.EmailAddress,
                                  U.Gender,
                                  U.ProfilePicURL AS [Image],
                                  M.TotalRows
                              FROM CTE_MEMBER M
                              INNER JOIN [User] U ON U.MemberDocId = M.EntityId
                              INNER JOIN ProcessInfo P ON P.PrimaryDocId = M.EntityId
                              --INNER JOIN [State] S ON S.StateId = P.CurrentStateId
                              WHERE NOT EXISTS (
                                                               SELECT 1 
                                                               FROM CTE_EXCLUDESTSTUS ES 
                                                               WHERE ES.StateId = P.CurrentStateId )
                              ORDER BY M.RowNumber ASC;";

            var queryParameters = new DynamicParameters(); 
            //if(clubDocIds.Any())
            //{
            //    queryParameters.Add("@clubDocIds", clubDocIds);
            //}
            queryParameters.Add("@UserId", currentUserId);
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);
            queryParameters.Add("@Query", $@"%{request.Query}%");
            var rows =(await _readRepository.GetLazyRepository<MemberWithCountTO>().Value.GetListAsync(dataSQL, cancellationToken, queryParameters, null, "text")).ToList();
            //var rowsCount = (int)(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(countSql, cancellationToken, queryParameters, null, "text"));
            var totalCount = rows.Any() ? rows.First().TotalRows : 0;
            var mappedRows = _mapper.Map<List<MemberDTO>>(rows);

            return new PagedResult<MemberDTO>(){
                TotalCount = totalCount,
                Items = mappedRows
            };
        }
    }
}
