using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Preferences.GetOptInCurrentsBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.Preferences.Queries.GetUserPreferences
{

    public class GetUserPreferencesHandler : IRequestHandler<GetUserPreferencesQuery, string>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;

        public GetUserPreferencesHandler(LazyService<IReadRepository<dynamic>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<string> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
        {
            string sql = """
                         SELECT
                             ISNULL(up.PreferenceValue, pt.DefaultValue) AS PreferenceValue
                         FROM [dbo].[PreferenceTypes] pt
                         LEFT JOIN [User] u ON u.MemberDocId = @MemberDocId
                         LEFT JOIN [dbo].[UserPreferences] up 
                             ON up.UserID = u.UserID
                            AND up.PreferenceTypeID = pt.PreferenceTypeID
                            AND up.OrganizationID = @OrganizationID
                            AND up.IsActive = 1
                         WHERE pt.PreferenceTypeID = @PreferenceTypeID;
                         """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberDocId", request.MemberDocId, dbType: DbType.String);
            queryParameters.Add("@OrganizationID", request.OrganizationId, dbType: DbType.Int32);
            queryParameters.Add("@PreferenceTypeID", request.PreferenceTypeId, dbType: DbType.Int32);
            var result = (await _readRepository.Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text"));
            return (string)result! ?? string.Empty;
        }
    }
}
