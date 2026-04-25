using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Domain.Entities;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.Common.Queries.GetUploadedMemberData
{
    public class GetUploadedMemberDataHandler : IRequestHandler<GetUploadedMemberDataQuery, ResultUploadedMemberData?>
    {
        private readonly IReadRepositoryFactory _readRepository;

        public GetUploadedMemberDataHandler(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<ResultUploadedMemberData?> Handle(GetUploadedMemberDataQuery request, CancellationToken cancellationToken = default)
        {
            const string sql = """
                               SELECT UploadedMemberDataId, [UploadedMemberId], [MemberData]
                               FROM [dbo].ResultUploadedMemberData
                               WHERE UploadedMemberDataId = @UploadedMemberDataId
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedMemberDataId", request.UploadedMemberDataId);

            return await _readRepository.GetLazyRepository<ResultUploadedMemberData>().Value
                .GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
        }
    }
}
