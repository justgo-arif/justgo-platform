using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetCurrentPreferencesBySyncGuid
{
    public class GetCurrentPreferencesBySyncGuidHandler : IRequestHandler<GetCurrentPreferencesBySyncGuidQuery, CurrentPreference>
    {
        private readonly LazyService<IReadRepository<CurrentPreference>> _readRepository;
        public GetCurrentPreferencesBySyncGuidHandler(LazyService<IReadRepository<CurrentPreference>> readRepository)
        {
            _readRepository = readRepository;
        }

        const string sql = """
                       DROP TABLE IF EXISTS #OptInCurrent;
                       
                       SELECT oc.OptInId, oc.[Value] INTO #OptInCurrent
                       FROM OptInCurrent oc
                       INNER JOIN [User] u ON u.MemberDocId = oc.EntityId
                       WHERE u.[UserSyncId] = @MemberSyncGuId;
                       
                       SELECT 
                       om.Id               AS OptInMasterId,
                       om.OwnerType        AS OwnerType,
                       om.[Title]          AS Title,
                       om.[Description]    AS [Description],
                       om.[Status]         AS Status,
                       
                       og.Id               AS OptInGroupId,
                       og.OptInMasterId    AS OptInGroupMasterId,
                       og.[Name]           AS OptInGroupName,
                       og.[Description]    AS OptInGroupDescription,
                       og.[Sequence]       AS OptInGroupSequence,
                       
                       o.Id                AS OptInId,
                       o.OptInGroupId      AS OptInGroupRefId,
                       o.Caption           AS Caption,
                       o.[Name]            AS OptInName,
                       o.[Description]     AS OptInDescription,
                       o.[Status]          AS OptInStatus,
                       o.[Sequence]        AS OptInSequence,
                       oc.[Value]          AS Selected

                       FROM OptInMaster om
                       LEFT JOIN OptInGroup og ON om.Id = og.OptInMasterId
                       LEFT JOIN OptIn o ON og.Id = o.OptInGroupId
                       LEFT JOIN #OptInCurrent oc ON oc.OptInId = o.Id;
                       """;
        public async Task<CurrentPreference> Handle(GetCurrentPreferencesBySyncGuidQuery request, CancellationToken cancellationToken)
        {
            var p = new DynamicParameters();
            p.Add("@MemberSyncGuId", request.SyncGuid);

            var resultList = await _readRepository.Value.GetListMultiMappingAsync<PreferenceMaster, PreferencesGroup, Preference>(
                sql,
                cancellationToken,
                "OptInMasterId",
                (master, group, optIn) =>
                {
                    master.PreferencesGroups ??= new List<PreferencesGroup>();

                    if (group == null || group.OptInGroupId == 0)
                        return master;

                    var existingGroup = master.PreferencesGroups.FirstOrDefault(g => g.OptInGroupId == group.OptInGroupId);
                    if (existingGroup == null)
                    {
                        existingGroup = group;
                        existingGroup.Preferences = new List<Preference>();
                        master.PreferencesGroups.Add(existingGroup);
                    }
                    else if (existingGroup.Preferences == null)
                    {
                        existingGroup.Preferences = new List<Preference>();
                    }

                    if (optIn == null || optIn.OptInId == 0)
                        return master;

                    if (!existingGroup.Preferences.Any(o => o.OptInId == optIn.OptInId))
                        existingGroup.Preferences.Add(optIn);

                    return master;
                },
                p,
                null,
                splitOn: "OptInGroupId,OptInId",
                commandType: "text"
            );

            var currentPreference = new CurrentPreference
            {
                MemberSyncGuid = request.SyncGuid.ToString(), 
                PreferenceMasters = resultList
            };

            return currentPreference;
        }
    }
}
