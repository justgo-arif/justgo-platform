using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionLicensesRules
{
    class SessionLicensesRulesQueryHandler : IRequestHandler<SessionLicensesRulesQuery, bool>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public SessionLicensesRulesQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<bool> Handle(SessionLicensesRulesQuery request, CancellationToken cancellationToken)
        {
           
            string sql = @"SELECT ISNULL(so.MembershipIds, '') AS MembershipIds

                FROM JustGoBookingClassSessionOption so
                INNER JOIN JustGoBookingClassSession cs 
                    ON so.SessionId = cs.SessionId
                INNER JOIN JustGoBookingClassSessionSchedule ss
                    ON so.SessionId = ss.SessionId
                INNER JOIN JustGoBookingScheduleOccurrence oc
                    ON ss.SessionScheduleId = oc.ScheduleId
                WHERE oc.OccurrenceId = @OccurrenceId;";

            var queryParam = new DynamicParameters();
            queryParam.Add("OccurrenceId", request.OccurrenceId);
            var result = await _readRepository.Value.GetSingleAsync(sql, queryParam, null, "text");


            string licenceSql = @"SELECT 
            mld.Entityid AS EntityDocId,
            mld.DocId AS LicenseDocId
         
            FROM MembersLicense_Default mld
                INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
                INNER JOIN [state] st ON st.StateId = pri.CurrentStateId
                INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
                INNER JOIN Document d ON d.DocId = pd.DocId
                INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
                INNER JOIN License_Default ld ON ld.DocId = ll.DocId
                INNER JOIN MembersLicense_Links mll ON mll.DocId = mld.DocId
            WHERE 
                ll.Entityparentid = 11 
                AND mld.Entityid = @MemberDocId 
                AND ld.DocId IN (SELECT value FROM STRING_SPLIT(@MembershipIds, ','))
            ORDER BY mld.EndDate DESC;";

            var queryParams = new DynamicParameters();
            queryParams.Add("MemberDocId", request.MemberDocId.ToString());
            queryParams.Add("MembershipIds",  result); 
            var resultLicence = await _readRepository.Value.GetListAsync(licenceSql, queryParams, null, "text");


            return CompareMissingLicence(result, resultLicence);
        }

        private bool CompareMissingLicence(object licenceIds,IEnumerable<object> licenceList)
        {
            if (licenceIds == "") return true;
            if (licenceIds != null || licenceList.Count() == 0) return false;

            //later use case
            //string[] ids = licenceIds.ToString().Split(',');
            //ids = ids.Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();

            return  licenceList.Count()>0;
        }
    }
}
