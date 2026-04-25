using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetTournamentRatingOverTime;

public class GetTournamentRatingOverTimeQueryHandler : IRequestHandler<GetTournamentRatingOverTimeQuery, Result<List<TournamentRatingOverTimeResponse>>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;

    public GetTournamentRatingOverTimeQueryHandler(
        IReadRepositoryFactory readRepositoryFactory,
        IUtilityService utilityService)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _utilityService = utilityService;
    }

    public async Task<Result<List<TournamentRatingOverTimeResponse>>> Handle(
        GetTournamentRatingOverTimeQuery request,
        CancellationToken cancellationToken = default)
    {
        var repo = _readRepositoryFactory.GetLazyRepository<object>().Value;

        var response = await GetPlayerProfileAsync(repo, request, cancellationToken);

        return response != null ? response : new List<TournamentRatingOverTimeResponse>();
    }

    private async Task<List<TournamentRatingOverTimeResponse>> GetPlayerProfileAsync(
         IReadRepository<object> repo,
        GetTournamentRatingOverTimeQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = await _utilityService.GetOwnerIdByGuid(request.OwnerGuid!, cancellationToken);
        var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeGuid, cancellationToken);

        var sql = BuildPlayerProfileQuery(ownerId);
        var parameters = BuildParameters(request, resultEventTypeId, ownerId);

        var result = await repo.GetListAsync<TournamentRatingOverTimeResponse>(sql, parameters, null, QueryType.Text, cancellationToken);
        return result.ToList();
    }

    private static string BuildPlayerProfileQuery(int ownerId)
    {
        //string categorySqlCondition = string.Empty;
        //if (ownerId >= 0)
        //{
        //    categorySqlCondition = " AND E.OwnerId = @OwnerId ";
        //}
        return $"""
           DECLARE @PlayerUserId INT = (SELECT TOP 1 UserId FROM [User] WHERE UserSyncId = @PlayerId);
           DECLARE @RankingType NVARCHAR(100) = (SELECT TOP 1 RankingType FROM ResultEventType WHERE ResultEventTypeId = @ResultEventTypeId);
           
           
           DECLARE @FromDate DATE = TRY_CONVERT(DATE, @FromDateInput);
           DECLARE @ToDate DATE   = TRY_CONVERT(DATE, @ToDateInput);
           
           SET @FromDate = ISNULL(@FromDate, 
           (
               SELECT MIN(E.StartDate)
               FROM ResultCompetitionRankings R
               INNER JOIN ResultCompetition C 
                   ON C.CompetitionId = R.CompetitionId 
                   AND C.IsDeleted = 0 
                   AND C.CompetitionStatusId = 2
               INNER JOIN ResultEvents E 
                   ON E.EventId = C.EventId
               WHERE R.UserId = @PlayerUserId
                 AND R.RankingType = @RankingType
                 AND E.ResultEventTypeId = @ResultEventTypeId
           ));
           
           SET @ToDate = ISNULL(@ToDate, 
           (
               SELECT MAX(E.StartDate)
               FROM ResultCompetitionRankings R
               INNER JOIN ResultCompetition C 
                   ON C.CompetitionId = R.CompetitionId 
                   AND C.IsDeleted = 0 
                   AND C.CompetitionStatusId = 2
               INNER JOIN ResultEvents E 
                   ON E.EventId = C.EventId
               WHERE R.UserId = @PlayerUserId
                 AND R.RankingType = @RankingType
                 AND E.ResultEventTypeId = @ResultEventTypeId
           ));
           
           DECLARE @MinDate DATE = @FromDate;
           DECLARE @MaxDate DATE = @ToDate;
           
           
           ;WITH InitialRating AS (
               SELECT TOP 1  R.FinalRating AS Rating
               FROM ResultCompetitionRankings R
               INNER JOIN ResultCompetition C  ON C.CompetitionId = R.CompetitionId AND C.IsDeleted = 0 AND C.CompetitionStatusId = 2
               INNER JOIN ResultEvents E  ON E.EventId = C.EventId
               WHERE R.UserId = @PlayerUserId
                 AND R.RankingType = @RankingType
                 AND E.ResultEventTypeId = @ResultEventTypeId
                 AND E.StartDate < @MinDate
               ORDER BY E.StartDate DESC
           ),
           RatingData AS (
               SELECT 
                   DATEFROMPARTS(YEAR(E.StartDate), MONTH(E.StartDate), 1) AS MonthStart,
                   MAX(R.FinalRating) AS Rating
               FROM ResultCompetitionRankings R
               INNER JOIN ResultCompetition C  ON C.CompetitionId = R.CompetitionId AND C.IsDeleted = 0 AND C.CompetitionStatusId = 2
               INNER JOIN ResultEvents E  ON E.EventId = C.EventId
               WHERE R.UserId = @PlayerUserId
                 AND R.RankingType = @RankingType
                 AND E.ResultEventTypeId = @ResultEventTypeId
                 AND E.StartDate BETWEEN @MinDate AND @MaxDate
               GROUP BY DATEFROMPARTS(YEAR(E.StartDate), MONTH(E.StartDate), 1)
           ),
           
           MonthRange AS (
               SELECT DATEFROMPARTS(YEAR(@MinDate), MONTH(@MinDate), 1) AS MonthStart
               UNION ALL
               SELECT DATEADD(MONTH, 1, MonthStart)
               FROM MonthRange
               WHERE DATEADD(MONTH, 1, MonthStart) <= @MaxDate
           ),
           Merged AS (
               SELECT 
                   M.MonthStart,
                   R.Rating
               FROM MonthRange M
               LEFT JOIN RatingData R
                   ON M.MonthStart = R.MonthStart
           ),
           FinalData AS (
               SELECT 
                   M.MonthStart,
                   CASE 
                       WHEN M.Rating IS NULL 
                            AND M.MonthStart = (SELECT MIN(MonthStart) FROM MonthRange)
                       THEN (SELECT Rating FROM InitialRating)
                       ELSE M.Rating
                   END AS Rating
               FROM Merged M
           )
           
           SELECT 
               FORMAT(MonthStart,'MMM') + '''' + RIGHT(CAST(YEAR(MonthStart) AS VARCHAR),2) AS Name,
               LAST_VALUE(NULLIF(Rating,0)) IGNORE NULLS 
                   OVER (ORDER BY MonthStart ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS Value
           FROM FinalData
           ORDER BY MonthStart
           OPTION (MAXRECURSION 0); 
        """;
    }

    private static DynamicParameters BuildParameters(
        GetTournamentRatingOverTimeQuery request,
        int? resultEventTypeId,
        int ownerId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@PlayerId", request.PlayerGuid);
        parameters.Add("@ResultEventTypeId", resultEventTypeId);
        parameters.Add("@FromDateInput", string.IsNullOrWhiteSpace(request.FromDate) ? null : request.FromDate.Trim());
        parameters.Add("@ToDateInput", string.IsNullOrWhiteSpace(request.ToDate) ? null : request.ToDate.Trim());
        parameters.Add("@OwnerId", ownerId < 0 ? null : ownerId);

        return parameters;
    }
}