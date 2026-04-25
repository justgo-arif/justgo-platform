using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.Common.Queries.GetDisciplineByFileId
{
    public class GetDisciplineByFileIdHandler : IRequestHandler<GetDisciplineByFileIdQuery, int>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetDisciplineByFileIdHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<int> Handle(GetDisciplineByFileIdQuery request, CancellationToken cancellationToken = default)
        {
            const string sql = """
                               select DisciplineId from ResultUploadedFile where UploadedFileId = @UploadedFileId and IsDeleted = 0
                               """;
            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", request.FileId);

            var result = await _readRepository.GetLazyRepository<object>().Value
                .GetSingleAsync<int>(sql, parameters, null, cancellationToken, QueryType.Text);
            return result;
        }
    }
}
