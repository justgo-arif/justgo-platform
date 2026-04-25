using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using System.Data;


namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetOwnerUserbyContactGuid
{
    public class GetOwnerUserbyContactGuidHandler : IRequestHandler<GetOwnerUserbyContactGuidQuery, UserBasicInfoDTO>
    {
        private readonly LazyService<IReadRepository<UserBasicInfoDTO>> _readRepository;

        public GetOwnerUserbyContactGuidHandler(LazyService<IReadRepository<UserBasicInfoDTO>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<UserBasicInfoDTO> Handle(GetOwnerUserbyContactGuidQuery request, CancellationToken cancellationToken)
        {
            string sql = """
                SELECT TOP 1 u.UserSyncId Id, u.UserId,u.MemberDocId,u.MemberId,u.[FirstName],u.[LastName]
                FROM [dbo].[UserEmergencyContacts] ec
                INNER JOIN [dbo].[User] u ON ec.UserId=u.Userid
                WHERE ec.[RecordGuid] = @RecordGuid
                """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RecordGuid", request.Id, dbType: DbType.Guid);

            var result = await _readRepository.Value.GetListAsync<UserBasicInfoDTO>(sql, queryParameters, null, "text", cancellationToken);
            return result.FirstOrDefault();
        }
    }
}
