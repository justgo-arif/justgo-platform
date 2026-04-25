using Dapper;
using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MapsterMapper;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetClubsMetadata
{
    public class GetClubsMetadataHandler : IRequestHandler<GetClubsMetadataQuery, PagedResult<ClubDTO>>
    {

        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IMapper _mapper;
        public GetClubsMetadataHandler(
            IReadRepositoryFactory readRepository, 
            IUtilityService utilityService,
            IMapper mapper)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
            _mapper = mapper;
        }


        public async Task<PagedResult<ClubDTO>> Handle(GetClubsMetadataQuery request, CancellationToken cancellationToken)
        {

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            string countSql = $@"SELECT Count(DISTINCT cd.DocId) TotalRowCount
                                FROM Clubs_Default cd
                                INNER JOIN [Document] d on d.DocId = cd.DocId 
						        INNER JOIN ClubMemberRoles cmr on cmr.ClubDocId = cd.DocId 
                                INNER JOIN [User] u on u.UserId = cmr.UserId
                                WHERE u.UserId = {currentUserId} and 
                                (cd.Clubname like @query or cd.ClubID like @query)";

            string sql = $@"SELECT DISTINCT 
							d.SyncGuid ClubId,
							cd.DocId ClubDocId,
							cd.Clubname ClubName,
							cd.[Location] ClubImage,
							cd.ClubID ClubReferenceId
                            FROM Clubs_Default cd
                            INNER JOIN [Document] d on d.DocId = cd.DocId 
						    INNER JOIN ClubMemberRoles cmr on cmr.ClubDocId = cd.DocId 
                            INNER JOIN [User] u on u.UserId = cmr.UserId
                            WHERE u.UserId = {currentUserId} and 
                            (cd.Clubname like @query or cd.ClubID like @query)
                            Order By cd.Clubname
                            OFFSET (@PageNumber - 1) * @PageSize ROWS
                            FETCH NEXT @PageSize ROWS ONLY";


            string conditionSQL = "";
            if (!string.IsNullOrEmpty(request.Query))
            {
                conditionSQL = @" and  
                        (cd.Clubname like @query or cd.ClubID like @query)
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
                              CTE_TOTAL_CLUB AS (
                                  SELECT
                                      H.EntityId,
                                      ROW_NUMBER() OVER (ORDER BY H.EntityId ) AS RowNumber,
                                      COUNT(*) OVER () AS TotalCount
                                  FROM dbo.Hierarchies H
                                  INNER JOIN Clubs_Default CD ON CD.DocId = H.EntityId
                                  WHERE EXISTS (
                                      SELECT 1
                                      FROM @NodeIds nodes
                                      WHERE H.HierarchyId.IsDescendantOf(nodes.HierarchyId) = 1
                                  )
                                  {conditionSQL}
                                  ORDER BY RowNumber 
                                  OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY
                        
                              )

                            SELECT DISTINCT 
							d.SyncGuid ClubId,
							cd.DocId ClubDocId,
							cd.Clubname ClubName,
							cd.[Location] ClubImage,
							cd.ClubID ClubReferenceId
                            FROM Clubs_Default cd
                            INNER JOIN [Document] d on d.DocId = cd.DocId 
                            INNER JOIN CTE_TOTAL_CLUB ctc ON ctc.EntityId = cd.DocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", currentUserId);
            queryParameters.Add("@PageNumber", request.PageNumber);
            queryParameters.Add("@PageSize", request.PageSize);
            queryParameters.Add("@Query", $@"%{request.Query}%");
            var rows =(await _readRepository.GetLazyRepository<ClubWithCountDTO>().Value.GetListAsync(dataSQL, cancellationToken, queryParameters, null, "text")).ToList();
            //var rowsCount = (int)(await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(countSql, cancellationToken, queryParameters, null, "text"));
            var totalCount = rows.Any() ? rows.First().TotalRows : 0;
            var mappedRows = _mapper.Map<List<ClubDTO>>(rows);

            return new PagedResult<ClubDTO>(){
                TotalCount = totalCount,
                Items = mappedRows,
            };
        }
    }
}
