using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerRankings;

public class GetPlayerRankingsQueryHandler : IRequestHandler<GetPlayerRankingsQuery, Result<PlayerRankingsResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IUtilityService _utilityService;

    public GetPlayerRankingsQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        ISystemSettingsService systemSettingsService,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _systemSettingsService = systemSettingsService;
        _utilityService = utilityService;
    }

    public async Task<Result<PlayerRankingsResponse>> Handle(GetPlayerRankingsQuery request, CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);

        var (players, totalCount, hasMore) = await GetPlayerRankingsAsync(repo, request, ownerId, cancellationToken);

        var response = new PlayerRankingsResponse
        {
            Players = players,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            HasMore = hasMore
        };

        return response;
    }

    private async Task<(List<PlayerRankingDto> Players, int TotalCount, bool HasMore)> GetPlayerRankingsAsync(
        IReadRepository<object> repo,
        GetPlayerRankingsQuery request,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var searchTokens = !string.IsNullOrWhiteSpace(request.SearchTerm)
            ? NormalizeAndTokenize(request.SearchTerm)
            : new List<string>();

        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);
        var sql = await BuildPlayerRankingsQuery(request, ownerId, resultEventTypeId, searchTokens, cancellationToken);
        var parameters = BuildParameters(request, ownerId, resultEventTypeId, searchTokens);

        var queryResults = await repo.GetListAsync<PlayerRankingDto>(sql, parameters, null, QueryType.Text, cancellationToken);
        var resultList = queryResults.ToList();

        if (!resultList.Any())
        {
            return (new List<PlayerRankingDto>(), 0, false);
        }

        var hasMore = resultList.Count > request.PageSize;
        if (hasMore)
        {
            resultList.RemoveAt(resultList.Count - 1);
        }

        var totalCount = resultList.FirstOrDefault()?.TotalRecords ?? 0;

        return (resultList, totalCount, hasMore);
    }

    private async Task<string> BuildPlayerRankingsQuery(
        GetPlayerRankingsQuery request,
        int ownerId,
        int? resultEventTypeId,
        List<string> searchTokens,
        CancellationToken cancellationToken)
    {
        var siteAddress = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
        var baseImageUrl = string.IsNullOrEmpty(siteAddress) ? string.Empty : siteAddress.TrimEnd('/');
        var hasSearchTerm = searchTokens.Count > 0;

        var ownerWhereClause = ownerId > 0 ? "R.OwnerId = @OwnerId" : "1=1";
        var membershipWhereClause = ownerId > 0 ? "AND ISNULL(UM.LicenceOwner, 0) = @OwnerId" : string.Empty;
        var statusWhereClause = request.All != 1 ? $"INNER JOIN UserMemberships UM ON UM.UserId = U.UserId {membershipWhereClause} AND UM.StatusID = 62" : string.Empty;
        var clubWhereClause = ownerId > 0 ? "AND CMR.ClubDocId = @OwnerId" : string.Empty;

        var eventTypeWhereClause = resultEventTypeId.HasValue ? "AND R.ResultEventTypeId = @ResultEventTypeId" : string.Empty;

        var searchWhereClause = string.Empty;
        if (hasSearchTerm)
        {
            var tokenConditions = new List<string>();
            for (int i = 0; i < searchTokens.Count; i++)
            {
                tokenConditions.Add($"(CONCAT(U.LastName, ' ', U.FirstName) LIKE '%' + @SearchToken{i} + '%' OR U.MemberId LIKE '%' + @SearchToken{i} + '%')");
            }
            searchWhereClause = tokenConditions.Count > 0
                ? $"AND ({string.Join(" AND ", tokenConditions)})"
                : string.Empty;
        }

        var searchTable = hasSearchTerm
            ? "INNER JOIN [User] U ON U.UserId = D.UserId"
            : string.Empty;

        var genderWhereClause = !string.IsNullOrWhiteSpace(request.Gender) ? "AND U.Gender IN (SELECT value FROM STRING_SPLIT(@Gender, ','))" : string.Empty;
        var countyWhereClause = !string.IsNullOrWhiteSpace(request.County) ? "AND U.County IN (SELECT value FROM STRING_SPLIT(@County, ','))" : string.Empty;
        var ageWhereClause = !string.IsNullOrWhiteSpace(request.MinAge) ? "AND DATEDIFF(YEAR, U.DOB, GETDATE()) - CASE  WHEN DATEADD(YEAR, DATEDIFF(YEAR, U.DOB, GETDATE()), U.DOB) > GETDATE() THEN 1 ELSE 0 END BETWEEN @AgeFrom AND @AgeTo" : string.Empty;

        int numRows = request.PageSize + 1;

        var rankingType = await GetRankingTypeAsync(resultEventTypeId, cancellationToken);

        return $"""
            ;WITH CTE_R AS (
                SELECT 
                    RR.UserId, 
                    RR.FinalRating,
                    ROW_NUMBER() OVER (PARTITION BY RR.UserId ORDER BY R.EndDate DESC) AS RN
                FROM ResultEvents R
                INNER JOIN ResultCompetition C ON C.EventId = R.EventId AND C.IsDeleted = 0 AND C.CompetitionStatusId = 2
                INNER JOIN ResultCompetitionRankings RR ON RR.CompetitionId = C.CompetitionId AND RR.RankingType = '{rankingType}'
                INNER JOIN [User] U ON U.UserId = RR.UserId
                {statusWhereClause}
                WHERE {ownerWhereClause}
                {eventTypeWhereClause}
                {genderWhereClause}
                {countyWhereClause}
                {ageWhereClause}
            ),
            CTE_DATA AS (
                SELECT 
                    R.UserId,
                    R.FinalRating,
                    ROW_NUMBER() OVER (ORDER BY R.FinalRating DESC) AS RowNum,
                    RANK() OVER (ORDER BY R.FinalRating DESC) AS RankPosition
                FROM CTE_R R
                WHERE RN = 1
            ),
            FINAL_DATA AS (
                SELECT TOP ({numRows})
                    D.UserId,
                    D.FinalRating,
                    D.RankPosition,
                    D.RowNum,
                    COUNT(*) OVER() AS TotalRows
                FROM CTE_DATA D
                {searchTable}
                WHERE D.RowNum > @LastSeenId
                {searchWhereClause}
                ORDER BY D.RowNum
            ),
            WIN_MATCH_RESULT AS (
                SELECT E.UserId, COUNT(1) TotalWin
                FROM FINAL_DATA E
                INNER JOIN ResultCompetitionRoundParticipants P ON P.EntityId = E.UserId
                INNER JOIN ResultCompetitionMatches M ON M.CompetitionParticipantId = P.CompetitionParticipantId AND M.IsDeleted = 0
                INNER JOIN ResultCompetitionRounds RCR ON M.RoundId = RCR.CompetitionRoundId
                INNER JOIN ResultCompetitionInstance RCI ON RCR.InstanceId = RCI.InstanceId
                INNER JOIN ResultCompetition RC ON RC.CompetitionId = RCI.CompetitionId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                INNER JOIN ResultEvents R ON R.EventId = RC.EventId
                {eventTypeWhereClause}
                GROUP BY E.UserId
            ),
            LOSE_MATCH_RESULT AS (
                SELECT E.UserId, COUNT(1) TotalLose
                FROM FINAL_DATA E
                INNER JOIN ResultCompetitionRoundParticipants P ON P.EntityId = E.UserId
                INNER JOIN ResultCompetitionMatches M ON M.CompetitionParticipantId2 = P.CompetitionParticipantId AND M.IsDeleted = 0
                INNER JOIN ResultCompetitionRounds RCR ON M.RoundId = RCR.CompetitionRoundId
                INNER JOIN ResultCompetitionInstance RCI ON RCR.InstanceId = RCI.InstanceId
                INNER JOIN ResultCompetition RC ON RC.CompetitionId = RCI.CompetitionId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                INNER JOIN ResultEvents R ON R.EventId = RC.EventId
                 {eventTypeWhereClause}
                GROUP BY E.UserId
            ),
            CLUB_INFO AS (
                SELECT TOP 1 R.UserId, H.EntityName ClubName, H.EntityId ClubDocId
                FROM FINAL_DATA R
                INNER JOIN ClubMemberRoles CMR ON CMR.UserId = R.UserId {clubWhereClause} AND CMR.IsPrimary = 1
                INNER JOIN Hierarchies H ON H.EntityId = CMR.ClubDocId
            )
            SELECT 
                R.UserId, 
                R.FinalRating, 
                R.RankPosition RatingRank,
                cast(U.UserSyncId as nvarchar(50)) AS PlayerId,
                CONCAT(U.LastName, ', ', U.FirstName) PlayerName, 
                U.MemberId, 
                CASE 
                    WHEN U.ProfilePicURL IS NOT NULL AND U.ProfilePicURL <> '' 
                    THEN '{baseImageUrl}/store/downloadPublic?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.UserId AS nvarchar(50))
                    ELSE ''
                END AS PlayerImageUrl,
                U.County AS [Address], 
                U.Country, 
                U.Gender,
                C.ClubName, 
                C.ClubDocId, 
                (ISNULL(W.TotalWin, 0) + ISNULL(L.TotalLose, 0)) TotalMatches, 
                ISNULL(W.TotalWin, 0) TotalWins, 
                ISNULL(L.TotalLose, 0) TotalLoses,
                R.TotalRows TotalRecords
            FROM FINAL_DATA R
            INNER JOIN [User] U ON U.UserId = R.UserId
            LEFT JOIN CLUB_INFO C ON C.UserId = R.UserId
            LEFT JOIN WIN_MATCH_RESULT W ON W.UserId = R.UserId
            LEFT JOIN LOSE_MATCH_RESULT L ON L.UserId = R.UserId
            ORDER BY R.RowNum ASC
            OPTION(OPTIMIZE FOR UNKNOWN);
            """;
    }

    private static DynamicParameters BuildParameters(
        GetPlayerRankingsQuery request,
        int ownerId,
        int? resultEventTypeId,
        List<string> searchTokens)
    {
        var parameters = new DynamicParameters();

        int lastSeenId = (request.PageNumber - 1) * request.PageSize;
        parameters.Add("@PageSize", request.PageSize + 1);
        parameters.Add("@LastSeenId", lastSeenId);

        if (ownerId > 0)
        {
            parameters.Add("@OwnerId", ownerId);
        }

        if (searchTokens.Count > 0)
        {
            for (int i = 0; i < searchTokens.Count; i++)
            {
                parameters.Add($"@SearchToken{i}", searchTokens[i]);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            parameters.Add("@Gender", request.Gender.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.County))
        {
            parameters.Add("@County", request.County.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.MinAge))
        {
            parameters.Add("@AgeFrom", request.MinAge.Trim());
        }
        if (!string.IsNullOrWhiteSpace(request.MaxAge))
        {
            parameters.Add("@AgeTo", request.MaxAge.Trim());
        }

        if (resultEventTypeId.HasValue)
        {
            parameters.Add("@ResultEventTypeId", resultEventTypeId.Value);
        }

        return parameters;
    }

    private async Task<string> GetRankingTypeAsync(int? resultEventTypeId, CancellationToken cancellationToken)
    {
        if (!resultEventTypeId.HasValue)
            return "Rating";

        var rankingType = await _readRepositoryFactory.GetLazyRepository<object>().Value.QueryFirstAsync<string>(
          "SELECT RankingType FROM ResultEventType WHERE ResultEventTypeId = @ResultEventTypeId",
          new { ResultEventTypeId = resultEventTypeId },
          null,
          QueryType.Text,
          cancellationToken);

        return string.IsNullOrWhiteSpace(rankingType) ? "Rating" : rankingType;
    }

    private static List<string> NormalizeAndTokenize(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<string>();
        }

        var normalized = searchTerm.Trim()
            .Replace(",", " ")
            .Replace("  ", " ");

        var tokens = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        return tokens;
    }
}
