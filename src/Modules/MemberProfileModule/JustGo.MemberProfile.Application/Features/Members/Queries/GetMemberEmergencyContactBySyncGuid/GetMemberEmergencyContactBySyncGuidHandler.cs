using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberEmergencyContactBySyncGuid
{
    public class GetMemberEmergencyContactBySyncGuidHandler : IRequestHandler<GetMemberEmergencyContactBySyncGuidQuery, IEnumerable<UserEmergencyContact>>
    {
        private readonly LazyService<IReadRepository<UserEmergencyContact>> _readRepository;

        public GetMemberEmergencyContactBySyncGuidHandler(LazyService<IReadRepository<UserEmergencyContact>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<IEnumerable<UserEmergencyContact>> Handle(GetMemberEmergencyContactBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT ec.[Id]
                                  ,ec.[UserId]
                                  ,ec.[FirstName]
                                  ,ec.[LastName]
                                  ,ec.[Relation]
                                  ,ec.[ContactNumber]
                                  ,ec.[EmailAddress]
                                  ,ec.[IsPrimary]
                                  ,ec.[CountryCode]
                                  ,ec.[RecordGuid]
                            FROM [dbo].[UserEmergencyContacts] ec
	                        INNER JOIN [dbo].[User] u ON ec.UserId=u.Userid
                            WHERE u.[UserSyncId] = @UserSyncId
                            ORDER BY Id DESC";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", request.Id, dbType: DbType.Guid);

            var result = await _readRepository.Value.GetListAsync<UserEmergencyContact>(sql, queryParameters, null, "text", cancellationToken);

            return result;
        }
    }
}
