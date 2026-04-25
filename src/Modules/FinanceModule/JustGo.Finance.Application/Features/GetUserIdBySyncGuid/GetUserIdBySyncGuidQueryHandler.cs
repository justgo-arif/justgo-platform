using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.GetUserIdBySyncGuid
{
    public class GetUserIdBySyncGuidQueryHandler : IRequestHandler<GetUserIdBySyncGuidQuery, int>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;

        public GetUserIdBySyncGuidQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<int> Handle(GetUserIdBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            var userIdResult = await _readRepository.Value
                .GetSingleAsync(
                    SqlQueries.SelectUserIdBySyncGuid,
                    cancellationToken,
                    QueryHelpers.GetGuidParams(request.SyncGuid),
                    null,
                    "text"
                );

            if (userIdResult is null)
                throw new InvalidOperationException($"User Id not found for the provided SyncGuid: {request.SyncGuid}");

            return Convert.ToInt32(userIdResult);
        }
    }
}
