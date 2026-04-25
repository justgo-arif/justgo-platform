using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember
{
    public class AddFamilyMemberHandler : IRequestHandler<AddFamilyMemberCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;

        public AddFamilyMemberHandler(IWriteRepositoryFactory writeRepositoryFactory)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
        }

        public async Task<int> Handle(AddFamilyMemberCommand request, CancellationToken cancellationToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Name", request.Name);
            parameters.Add("@FamilyDocId", request.FamilyDocId);
            parameters.Add("@ClubDocId", request.ClubDocId);
            parameters.Add("@UserId", request.UserId);
            parameters.Add("@MemberDocIds", request.MemberDocIds);

            // Execute stored procedure; returns number of affected rows
            var rowsAffected = await _writeRepositoryFactory
                .GetLazyRepository<object>()
                .Value
                .ExecuteAsync("AddFamilyMember", cancellationToken, parameters);

            return rowsAffected;
        }
    }
}
