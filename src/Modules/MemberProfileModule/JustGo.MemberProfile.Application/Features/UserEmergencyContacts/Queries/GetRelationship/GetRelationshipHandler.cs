using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetRelationship
{
    public class GetRelationshipHandler : IRequestHandler<GetRelationshipQuery, List<UserRelationshipDto>>
    {
        private readonly LazyService<IReadRepository<UserRelationshipDto>> _readRepository;

        public GetRelationshipHandler(LazyService<IReadRepository<UserRelationshipDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<UserRelationshipDto>> Handle(GetRelationshipQuery request, CancellationToken cancellationToken)
        {
            string sql = @"
                         DECLARE @Sql varchar(5000) = (SELECT dbo.GetLookupTableQuery('Relation'))
                                        SET @Sql = '
                                        WITH LU AS ('
                                        +@Sql+
                                        ')
                                        SELECT RowId, [Name]
                                        FROM LU 
                                        '
                                        EXECUTE(@Sql) ;";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();

            return result;
        }
    }
}

