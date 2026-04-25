using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Json.Schema;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionLicensesRules
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
            bool isPassed = false;

            string sql = @"SELECT  
                TRY_CAST(JSON_VALUE(m.value, '$.OwnerId') AS INT) AS OwnerId,
                TRY_CAST(JSON_VALUE(m.value, '$.LicenseDocId') AS INT) AS LicenseDocId,
                JSON_QUERY(m.value, '$.ClassificationLicenseDocId') AS ClassificationLicenseDocId,
                ISNULL(LTRIM(RTRIM(TRY_CAST(JSON_VALUE(so.MembershipConfig, '$.ConditionType') AS nvarchar(20)))), '') AS ConditionType,
                CASE
                    WHEN LTRIM(RTRIM(
                        ISNULL(TRY_CAST(JSON_VALUE(so.MembershipConfig, '$.ConditionType') AS nvarchar(20)), '')
                    )) <> ''
                    THEN 1
                    ELSE 0
                END AS IsConditionType,
                so.SessionId
            FROM JustGoBookingClassSessionOption so
            INNER JOIN JustGoBookingClassSession cs ON so.SessionId = cs.SessionId
            INNER JOIN JustGoBookingClassSessionSchedule ss ON so.SessionId = ss.SessionId
            INNER JOIN JustGoBookingScheduleOccurrence oc ON ss.SessionScheduleId = oc.ScheduleId
            CROSS APPLY OPENJSON(so.MembershipConfig, '$.LicenseInfo') AS m
            WHERE 
                oc.OccurrenceId = @OccurrenceId
                AND ISJSON(so.MembershipConfig) = 1
                AND NULLIF(LTRIM(RTRIM(so.MembershipConfig)), '') IS NOT NULL
                Order by so.SessionOptionId;";

            var queryParam = new DynamicParameters();
            queryParam.Add("OccurrenceId", request.OccurrenceId);


            var result = await _readRepository.Value.GetListAsync(sql, queryParam, null, "text");
            if (result.Count()==0) return true;// if null then return without checking membership

            var castList = JsonConvert.DeserializeObject<List<LicenseInfoModel>>(JsonConvert.SerializeObject(result));


            bool isTypeFound = castList.Any(m => m.IsConditionType);
            string type = castList.FirstOrDefault().ConditionType;
            // Unique owner count to compare with valid owner count
            var uniqueOwnerCount = castList
                .GroupBy(x => x.OwnerId)
                .Count();
            int validCount = 0; //according to owner quantity
            foreach (var item in castList.ToList())
            {
               
                List<int> ids = new List<int>();
                ids.AddRange(item.LicenseDocId);
                ids.AddRange(JsonConvert.DeserializeObject<List<int>>(item.ClassificationLicenseDocId)
                                ?? new List<int>());


                string licenceSql = GetLicenseQuery();

                var queryParams = new DynamicParameters();
                queryParams.Add("MemberDocId", request.MemberDocId.ToString());
                queryParams.Add("MembershipIds", ids);

                var resultCount = await _readRepository.Value.GetSingleAsync<int>(
                    licenceSql, queryParams, null,cancellationToken, "text");
            
                // any owner license can be valid and count will increase
                //when unique ids count equals to valid unique license count counter will increase
                if (ids.Count ==resultCount) validCount++;

            }
            if (uniqueOwnerCount == 1 && validCount > 0) isPassed = true;
            else if (isTypeFound)
            {
                isPassed = (isTypeFound && type == "Or") ? (validCount > 0) : validCount == uniqueOwnerCount;
            }
            else
            {
                isPassed = validCount > 0;
            }
            return isPassed;
        }


        protected string GetLicenseQuery()
        {
            return @"SELECT COUNT(*) 
            FROM MembersLicense_Default mld
            INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mld.DocId
            INNER JOIN [state] st ON st.StateId = pri.CurrentStateId
            INNER JOIN Products_Default pd ON pd.DocId = mld.LicenceCode
            INNER JOIN Document d ON d.DocId = pd.DocId
            INNER JOIN License_Links ll ON ll.Entityid = pd.DocId
            INNER JOIN License_Default ld ON ld.DocId = ll.DocId
            INNER JOIN MembersLicense_Links mll ON mll.DocId = mld.DocId
            LEFT JOIN clubs_default cd ON cd.docid = ld.LicenceOwner AND ld.LicenceOwner <> 0
        WHERE 
            ll.Entityparentid = 11 
            AND mld.Entityid = @MemberDocId 
            AND ld.DocId IN @MembershipIds
            AND Cast(mld.EndDate as Date)>= Cast(GETUTCDATE() as Date)
         GROUP BY mld.Entityid
        ";
        }
    }
}
