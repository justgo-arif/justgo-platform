using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInCurrentsBySyncGuid
{
    public class GetOptInCurrentsBySyncGuidHandler : IRequestHandler<GetOptInCurrentsBySyncGuidQuery, List<OptInCurrent>>
    {
        private readonly LazyService<IReadRepository<OptInCurrent>> _readRepository;

        public GetOptInCurrentsBySyncGuidHandler(LazyService<IReadRepository<OptInCurrent>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<OptInCurrent>> Handle(GetOptInCurrentsBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT oc.* 
                                FROM OptInCurrent oc 
	                                INNER JOIN optin oi ON oc.OptInId=oi.Id
                                    INNER JOIN OptInGroup og ON og.Id=oi.OptInGroupId
                                    INNER JOIN OptInMaster om ON om.Id=og.OptInMasterId
	                                INNER JOIN [User] u ON u.MemberDocId=oc.EntityId
                                WHERE u.[UserSyncId] = @UserSyncId 
	                                AND oi.[Status]=1";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", request.SyncGuid, dbType: DbType.Guid);
            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
