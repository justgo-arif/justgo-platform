using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.DTOs.Enums;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyRequestDetails
{
    public sealed class GetFamilyRequestDetailsQueryHandler : IRequestHandler<GetFamilyRequestDetailsQuery, List<FamilyRequestDetailsDto>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;

        public GetFamilyRequestDetailsQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<FamilyRequestDetailsDto>> Handle(GetFamilyRequestDetailsQuery request, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    uf.RecordGuid,
                    f.FamilyName,
                    CASE 
                        WHEN ISNULL(U.ProfilePicURL,'')!='' THEN '/store/download?f=' + U.ProfilePicURL + '&t=user&p=' + CAST(U.Userid AS NVARCHAR(50))
                        ELSE '' END AS ProfilePicURL,
                    CASE 
                        WHEN DATEDIFF(DAY, uf.JoinDate, GETDATE()) = 1 THEN '1 day ago'
                        WHEN DATEDIFF(DAY, uf.JoinDate, GETDATE()) BETWEEN 2 AND 6 THEN CAST(DATEDIFF(DAY, uf.JoinDate, GETDATE()) AS VARCHAR(10)) + ' days ago'
                        WHEN DATEDIFF(DAY, uf.JoinDate, GETDATE()) BETWEEN 7 AND 13 THEN '1 week ago'
                        WHEN DATEDIFF(DAY, uf.JoinDate, GETDATE()) BETWEEN 14 AND 27 THEN CAST(DATEDIFF(WEEK, uf.JoinDate, GETDATE()) AS VARCHAR(10)) + ' weeks ago'
                        WHEN DATEDIFF(MONTH, uf.JoinDate, GETDATE()) = 1 THEN '1 month ago'
                        WHEN DATEDIFF(MONTH, uf.JoinDate, GETDATE()) BETWEEN 2 AND 11 THEN CAST(DATEDIFF(MONTH, uf.JoinDate, GETDATE()) AS VARCHAR(10)) + ' months ago'
                        WHEN DATEDIFF(MONTH, uf.JoinDate, GETDATE()) >= 12 THEN 'long ago'
                        ELSE 'Today'
                    END AS RegisterDateAgo
                FROM UserFamilies uf 
                INNER JOIN Families f ON f.FamilyId = uf.FamilyId
                INNER JOIN UserFamilies ufAdmin ON ufAdmin.FamilyId = uf.FamilyId AND ufAdmin.IsAdmin = 1
                INNER JOIN [User] U ON U.Userid = ufAdmin.UserId
                WHERE uf.RecordGuid = @RecordGuid
            """;

            var parameters = new DynamicParameters();
            parameters.Add("@RecordGuid", request.RecordGuid, DbType.Guid);

            var rows = await _readRepository.Value.GetListAsync(sql, cancellationToken, parameters, null, "text");

            var result = rows.Select(static r =>
            {
                var dict = (IDictionary<string, object>)r;

                return new FamilyRequestDetailsDto
                {
                    RecordGuid = (Guid)dict["RecordGuid"],
                    FamilyName = dict["FamilyName"].ToString() ?? string.Empty,
                    ProfilePicURL = dict["ProfilePicURL"].ToString() ?? string.Empty,
                    RequestLogged= dict["RegisterDateAgo"].ToString() ?? string.Empty,
                };
            }).ToList();

            return result;
        }
    }
}

