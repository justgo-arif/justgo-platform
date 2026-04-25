using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
namespace JustGo.Membership.Application.Features.Memberships.Queries.GetFamilyByMemberDocId
{
    public class GetFamilyByMemberHandler : IRequestHandler<GetFamilyByMemberQuery, FamilyDetailsDto>
    {
        private readonly LazyService<IReadRepository<FamilyDetailsDto>> _readRepository;

        public GetFamilyByMemberHandler(LazyService<IReadRepository<FamilyDetailsDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<FamilyDetailsDto> Handle(GetFamilyByMemberQuery request, CancellationToken cancellationToken)
        {
            const string query = @"
            DECLARE @listStr VARCHAR(MAX)
            SELECT @listStr = COALESCE(@listStr + ',', '') + CONVERT(varchar(50), entityid)
            FROM Family_Links
            WHERE DocId IN (SELECT TOP 1 DocId FROM Family_Links WHERE entityid = @MemberDocId AND entityparentid = 1)
            AND Entityparentid = 1;

            SELECT DocId, Reference, Familyname, @listStr AS Members
            FROM Family_Default
            WHERE DocId IN (SELECT TOP 1 DocId FROM Family_Links WHERE entityid = @MemberDocId AND entityparentid = 1);";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberDocId", request.MemberDocId, dbType: DbType.Int32);

            var result = await _readRepository.Value.GetAsync(query, cancellationToken, queryParameters, null, "text");

            return result;
        }
    }
}