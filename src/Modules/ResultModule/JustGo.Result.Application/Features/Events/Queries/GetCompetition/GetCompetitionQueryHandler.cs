using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetCompetition;

public class GetCompetitionQueryHandler : IRequestHandler<GetCompetitionQuery, Result<GetCompetitionResponse>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public GetCompetitionQueryHandler(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<GetCompetitionResponse>> Handle(GetCompetitionQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = """
                SELECT
                    EventId,
                    EventName,
                    dbo.GET_UTC_LOCAL_DATE_TIME(StartDate, TimeZone) AS StartDate,
                    dbo.GET_UTC_LOCAL_DATE_TIME(EndDate,   TimeZone) AS EndDate,
                    CategoryId,
                    ResultEventTypeId,
                    TimeZone,
                    ImagePath,
                    Postcode,
                    County,
                    Town,
                    Address1,
                    Address2
                FROM ResultEvents
                WHERE EventId = @EventId;
                """;

            var parameters = new DynamicParameters();
            parameters.Add("@EventId", request.EventId);

            var readRepo = _readRepositoryFactory.GetRepository<GetCompetitionResponse>();

            var competition = await readRepo.GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);

            if (competition is null)
                return Result<GetCompetitionResponse>.Failure(
                    $"Competition with EventId {request.EventId} was not found.",
                    ErrorType.NotFound);

            return competition;
        }
        catch (Exception)
        {
            return Result<GetCompetitionResponse>.Failure(
                "An error occurred while retrieving the competition. Please try again.",
                ErrorType.InternalServerError);
        }
    }
}