using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.GetDocIdBySyncGuid
{
    public class GetDocIdBySyncGuidHandler : IRequestHandler<GetDocIdBySyncGuidQuery, int>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;

        public GetDocIdBySyncGuidHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<int> Handle(GetDocIdBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            var docIdObj = await _readRepository.Value
                .GetSingleAsync(
                    SqlQueries.SelectDocIdBySyncGuid,
                    cancellationToken,
                    QueryHelpers.GetGuidParams(request.SyncGuid),
                    null,
                    "text"
                );

            if (docIdObj is null)
                throw new InvalidOperationException($"Document Id not found for the provided SyncGuid: {request.SyncGuid}");

            return Convert.ToInt32(docIdObj);
        }
    }
}
