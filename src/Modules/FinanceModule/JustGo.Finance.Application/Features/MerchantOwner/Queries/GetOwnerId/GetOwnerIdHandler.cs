using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId
{
    public class GetOwnerIdHandler : IRequestHandler<GetOwnerIdQuery, int>
    {
        private readonly LazyService<IReadRepository<MerchantLookup>> _readRepository;

        public GetOwnerIdHandler(LazyService<IReadRepository<MerchantLookup>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<int> Handle(GetOwnerIdQuery request, CancellationToken cancellationToken)
        {
            var ownerIdValue = await _readRepository.Value.GetSingleAsync(
                    SqlQueries.GetOwnerIdSQL,
                    cancellationToken,
                    QueryHelpers.GetGuidParams(request.OwnerId),
                    null,
                    "text"
            );

            if (ownerIdValue == null)
            {
                throw new InvalidOperationException($"OwnerId for Guid '{request.OwnerId}' could not be found.");
            }
            return Convert.ToInt32(ownerIdValue);
        }
    }
}
