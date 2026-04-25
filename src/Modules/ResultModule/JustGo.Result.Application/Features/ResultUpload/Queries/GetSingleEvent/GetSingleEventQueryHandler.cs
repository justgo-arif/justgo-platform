using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetSingleEvent;

public class GetSingleEventQueryHandler : IRequestHandler<GetSingleEventQuery, Result<string>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public GetSingleEventQueryHandler(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<string>> Handle(GetSingleEventQuery request, CancellationToken cancellationToken = default)
    {
        const string query = "Select top 1 EventName from ResultEvents Where EventId = @EventId";

        var readRepository = _readRepositoryFactory.GetRepository<string>();

        var eventName = await readRepository.GetSingleAsync<string>(query, new { request.EventId }, null, cancellationToken,
            QueryType.Text);

        return eventName ?? Result<string>.Failure("Event not found.", ErrorType.NotFound);
    }
}