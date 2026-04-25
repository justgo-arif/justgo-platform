using Dapper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEvents.GetEquestrianEvents;

public class EquestrianEventsQueryProcessor : BaseEventsQueryProcessor
{
    public EquestrianEventsQueryProcessor(
        IReadRepositoryFactory readRepositoryFactory,
        IUtilityService utilityService,
        ISystemSettingsService systemSettingsService)
        : base(readRepositoryFactory, utilityService, systemSettingsService)
    {
    }

    protected override string GetParticipantDataCte()
    {
        return """
            ParticipantData AS (
                SELECT
                    fe.EventId,
                    rd.Name AS DisciplineName,
                    cp.UserId,
                    ca.AssetId
                FROM FilteredEvents fe
                INNER JOIN ResultCompetition rc ON rc.EventId = fe.EventId
                    AND rc.IsDeleted = 0
                    AND rc.CompetitionStatusId = 2
                INNER JOIN ResultDisciplines rd ON rc.DisciplineId = rd.DisciplineId
                INNER JOIN ResultCompetitionRounds cr ON rc.CompetitionId = cr.CompetitionId
                INNER JOIN ResultCompetitionParticipants cp ON cr.CompetitionRoundId = cp.CompetitionRoundId
                INNER JOIN ResultCompetitionAssets ca ON ca.CompetitionParticipantId = cp.CompetitionParticipantId
            )
            """;
    }

    protected override string GetParticipantCountColumns()
    {
        return """
            COUNT(DISTINCT UserId) AS TotalParticipants,
            COUNT(DISTINCT AssetId) AS TotalAssets
            """;
    }

    protected override string GetSelectColumns()
    {
        return """
            COALESCE(pc.TotalParticipants, 0) AS TotalParticipants,
            COALESCE(pc.TotalAssets, 0) AS TotalAssets
            """;
    }

    protected override (string BaseWhereClause, string DisciplineWhereClause, string SearchWhereClause) BuildWhereClause(
        GetEventsQuery request, int ownerId)
    {
        var baseConditions = new List<string>();
        var disciplineConditions = new List<string>();
        var searchConditions = new List<string>();

        if (ownerId >= 0)
            baseConditions.Add("AND re.OwnerId = @OwnerId");

        if (!string.IsNullOrWhiteSpace(request.Year))
            baseConditions.Add("AND YEAR(re.StartDate) IN (SELECT value FROM STRING_SPLIT(@Year, ','))");

        if (!string.IsNullOrWhiteSpace(request.EventCategory))
            baseConditions.Add("AND ed.EventCategory = @EventCategory");

        if (!string.IsNullOrWhiteSpace(request.DisciplineFilter))
         disciplineConditions.Add("AND rd.Name IN (SELECT value FROM STRING_SPLIT(@DisciplineFilter, ','))");


        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            searchConditions.Add("AND (EventName LIKE '%' + @SearchTerm + '%' OR DisciplineName LIKE '%' + @SearchTerm + '%')");

        return (
            string.Join(" ", baseConditions),
            string.Join(" ", disciplineConditions),
            string.Join(" ", searchConditions)
        );
    }

    protected override DynamicParameters BuildParameters(GetEventsQuery request, int ownerId)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@PageNumber", request.PageNumber);
        parameters.Add("@PageSize", request.PageSize);

        if (ownerId >= 0)
            parameters.Add("@OwnerId", ownerId);

        if (!string.IsNullOrWhiteSpace(request.Year))
            parameters.Add("@Year", request.Year.Trim());

        if (!string.IsNullOrWhiteSpace(request.EventCategory))
            parameters.Add("@EventCategory", request.EventCategory.Trim());

        parameters.Add("@DisciplineFilter",
            !string.IsNullOrWhiteSpace(request.DisciplineFilter) ? request.DisciplineFilter.Trim() : null);
        parameters.Add("@SearchTerm",
            !string.IsNullOrWhiteSpace(request.SearchTerm) ? request.SearchTerm.Trim() : null);

        return parameters;
    }
}