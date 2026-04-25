using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;


namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInsWithCurrent
{
    public class GetOptInsWithCurrentHandler : IRequestHandler<GetOptInsWithCurrentQuery, List<OptInMaster>>
    {
        private readonly LazyService<IReadRepository<OptInMaster>> _readRepository;

        public GetOptInsWithCurrentHandler(LazyService<IReadRepository<OptInMaster>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<OptInMaster>> Handle(GetOptInsWithCurrentQuery request, CancellationToken cancellationToken)
        {
            string sql = """
                DROP TABLE IF EXISTS #OptInCurrent

                SELECT oc.* INTO #OptInCurrent FROM OptInCurrent oc 
                INNER JOIN [User] u ON u.MemberDocId=oc.EntityId
                WHERE u.[UserSyncId] = @MemberSyncGuId 
                
                SELECT om.*
                ,CONVERT(VARCHAR(5),om.LastModifiedDate,108) LastModifiedTime
                ,og.*
                ,o.*
                ,oc.*
                FROM OptInMaster om
                LEFT JOIN OptInGroup og ON om.Id=og.OptInMasterId
                LEFT JOIN OptIn o ON og.Id=o.OptInGroupId
                LEFT JOIN #OptInCurrent oc ON oc.OptinId=o.Id 
                """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberSyncGuId", request.SyncGuid);

            var resultList = await _readRepository.Value.GetListMultiMappingAsync<OptInMaster, OptInGroup, OptIn, OptInCurrent>(
                sql,
                cancellationToken,
                "Id",
                (master, group, optIn, current) =>
                {
                    // Ensure the master collection is initialized
                    master.Groups ??= new List<OptInGroup>();

                    // Guard: if there is no real group (LEFT JOIN produced NULLs -> Id == 0), just return master.
                    if (group == null || group.Id == 0)
                        return master;

                    // Find or add group
                    var existingGroup = master.Groups.FirstOrDefault(g => g.Id == group.Id);
                    if (existingGroup == null)
                    {
                        existingGroup = group;
                        existingGroup.OptIns = new List<OptIn>();
                        master.Groups.Add(existingGroup);
                    }
                    else if (existingGroup.OptIns == null)
                    {
                        existingGroup.OptIns = new List<OptIn>();
                    }

                    // Guard for opt-in null / default
                    if (optIn == null || optIn.Id == 0)
                        return master;

                    // Add opt-in if not already present
                    if (!existingGroup.OptIns.Any(o => o.Id == optIn.Id))
                        existingGroup.OptIns.Add(optIn);

                    // Only assign Current if we actually have current data
                    if (current != null && current.Id != 0)
                    {
                        optIn.Current = current; // overwrite only when a concrete current row exists
                    }

                    return master;
                },
                queryParameters,
                null,
                splitOn: "Id,Id,Id",
                commandType: "text"
            );
            return resultList;
        }
    }
}


