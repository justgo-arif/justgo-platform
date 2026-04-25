using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.GetFieldList
{
    public class GetFieldListHandler
    : IRequestHandler<GetFieldListQuery, List<LookupIntDto>>
    {
        private readonly LazyService<IReadRepository<LookupIntDto>> _readRepository;

        public GetFieldListHandler(LazyService<IReadRepository<LookupIntDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<LookupIntDto>> Handle(
            GetFieldListQuery request,
            CancellationToken cancellationToken)
        {
            var sql = @"select FieldId as Id, Name from Field where FieldSetId =  @FieldSetId ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("FieldSetId", request.FieldSetId);

            var result = await _readRepository.Value
                .GetListAsync(sql, cancellationToken, queryParameters, null, "text");

            return result.ToList();
        }
    }

}
