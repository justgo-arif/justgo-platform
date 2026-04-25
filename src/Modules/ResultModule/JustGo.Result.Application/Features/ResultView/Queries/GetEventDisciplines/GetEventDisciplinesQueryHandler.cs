using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetEventDisciplines;

public class GetEventDisciplinesQueryHandler : IRequestHandler<GetEventDisciplinesQuery, 
    Result<ICollection<EventDisciplineDto>>>
{
    private readonly IReadRepository<EventDisciplineDto> _readRepository;

    public GetEventDisciplinesQueryHandler(IReadRepository<EventDisciplineDto> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<ICollection<EventDisciplineDto>>> Handle(GetEventDisciplinesQuery request,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT DISTINCT RD.DisciplineId, RD.[Name] AS DisciplineName
                           FROM ResultCompetition RC
                           INNER JOIN ResultDisciplines RD ON RC.DisciplineId = RD.DisciplineId
                           WHERE EventId = @EventId AND RC.IsDeleted = 0 AND RC.CompetitionStatusId = 2
                           """;

        return (await _readRepository.GetListAsync(sql, cancellationToken, new { request.EventId }, null,
            QueryType.Text)).ToList();
    }
}