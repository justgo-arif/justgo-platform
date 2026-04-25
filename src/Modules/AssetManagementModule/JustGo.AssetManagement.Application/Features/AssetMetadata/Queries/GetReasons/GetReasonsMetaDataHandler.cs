using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.AssetManagement.Application.Features.AssetMetadata.Queries.GetReasonsMetaData
{
    public class GetReasonsMetaDataHandler : IRequestHandler<GetReasonsMetaDataQuery, List<SelectListItemDTO<string>>>
    {

        private readonly IReadRepositoryFactory _readRepository;

        public GetReasonsMetaDataHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetReasonsMetaDataQuery request, CancellationToken cancellationToken)
        {
            return (await _readRepository.GetLazyRepository<SelectListItemDTO<string>>().Value.GetListAsync(
                     "Select ReasonName [Text], RecordGuid [Value] from ActionReason order  by ReasonName", cancellationToken, null, null, "text")).ToList();
        }
    }
}
