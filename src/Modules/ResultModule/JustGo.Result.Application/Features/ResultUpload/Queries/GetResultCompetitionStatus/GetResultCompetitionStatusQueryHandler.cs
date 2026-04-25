using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetResultCompetitionStatus;

public class GetResultCompetitionStatusQueryHandler : IRequestHandler<GetResultCompetitionStatusQuery, List<ResultCompetitionStatus>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public GetResultCompetitionStatusQueryHandler(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<List<ResultCompetitionStatus>> Handle(GetResultCompetitionStatusQuery request, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT CompetitionStatusId as StatusId, CompetitionStatusName as StatusName FROM ResultCompetitionStatus
                           """;
        
        var result = await _readRepositoryFactory.GetLazyRepository<ResultCompetitionStatus>().Value
            .GetListAsync(sql, cancellationToken, null, null, QueryType.Text);
        
        return result.ToList();
    }
}