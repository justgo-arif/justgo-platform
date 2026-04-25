using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetGender
{
    public class GetGenderHandler : IRequestHandler<GetGenderQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;

        public GetGenderHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetGenderQuery request, CancellationToken cancellationToken)
        {
            string sql = @"
               SELECT genderSplit.value AS Text
               FROM systemsettings s
               CROSS APPLY STRING_SPLIT(
                   JSON_VALUE(s.Value, '$.GenderOptions'), '|'
               ) AS genderSplit
               WHERE s.itemkey = 'ORGANISATION.GENDEROPTIONS';
                 ";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();
            return result;
        }
    }
}

