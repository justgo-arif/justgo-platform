using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetNonEditableMemberHeaders
{
    public class GetNonEditableMemberHeaderQueryHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetNonEditableMemberHeaderQuery, List<string>>
    {
        private const string SqlQuery = """
                                        select HeaderName from ResultNonEditableHeaders where isActive =1 and SportTypeId=@SportTypeId
                                        """;

        public async Task<List<string>> Handle(GetNonEditableMemberHeaderQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@SportTypeId", (int)request.SportType);
            var repo = readRepository.GetRepository<string>();
            var items = await repo.GetListAsync(SqlQuery, cancellationToken, queryParameters, null, QueryType.Text);
            return items.ToList();
        }
    }
}
