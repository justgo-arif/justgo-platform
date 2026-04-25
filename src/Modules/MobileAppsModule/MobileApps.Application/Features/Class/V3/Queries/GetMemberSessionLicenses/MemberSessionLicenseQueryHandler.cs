using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMemberSessionLicenses
{
    class MemberSessionLicenseQueryHandler:IRequestHandler<MemberSessionLicenseQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public MemberSessionLicenseQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(MemberSessionLicenseQuery request, CancellationToken cancellationToken)
        {

       

            string sql = @"SELECT DISTINCT 
                   TRY_CAST(JSON_VALUE(m.value, '$.LicenseDocId') AS INT) AS LicenseDocId,
                   JSON_QUERY(m.value, '$.ClassificationLicenseDocId') AS ClassificationLicenseDocId,
                   ISNULL(LTRIM(RTRIM(TRY_CAST(JSON_VALUE(so.MembershipConfig, '$.ConditionType') AS nvarchar(20)))), '') AS ConditionType,
                  CASE
                    WHEN LTRIM(RTRIM(
                        ISNULL(TRY_CAST(JSON_VALUE(so.MembershipConfig, '$.ConditionType') AS nvarchar(20)), '')
                    )) <> ''
                    THEN 1
                    ELSE 0
                 END AS IsConditionType

                FROM 
                    JustGoBookingClassSessionOption so
                    CROSS APPLY OPENJSON(so.MembershipConfig, '$.LicenseInfo') AS m
                WHERE 
                    so.SessionId = @SessionId AND 
                    ISJSON(so.MembershipConfig) = 1
                    AND NULLIF(LTRIM(RTRIM(so.MembershipConfig)), '') IS NOT NULL";

            var queryParam = new DynamicParameters();
            queryParam.Add("SessionId", request.SessionId);


            var result = await _readRepository.Value.GetListAsync(sql, queryParam, null, "text");
            if (result == null)return  new List<IDictionary<string, object>>();// if null then return without checking membership

            var castList = JsonConvert.DeserializeObject<List<LicenseInfoModel>>(JsonConvert.SerializeObject(result));

            //set type logic here
            List<int> ids = new List<int>();
           // bool isTypeFound = castList.Any(m => m.IsConditionType);

            foreach (var item in castList)
            {
                ids.AddRange(item.LicenseDocId);
                ids.AddRange(JsonConvert.DeserializeObject<List<int>>(item.ClassificationLicenseDocId));
            }
          
           //if(licenceOldsDocIds.Count>0)ids.AddRange(licenceOldsDocIds);  //old logic adjusted

            var queryParams = new DynamicParameters();
            queryParams.Add("MemberDocId", request.MemberDocId);
            queryParams.Add("MembershipIds", ids);

            var resultLicense = await _readRepository.Value.GetListAsync(GetLicenseQuery(), queryParams, null, "text");

            return JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(JsonConvert.SerializeObject(resultLicense))?? new List<IDictionary<string, object>>();
        }

        protected string GetLicenseQuery()
        {
            return @"SELECT DISTINCT
            mld.Entityid AS EntityDocId,
            mld.DocId AS LicenseDocId,
            mld.Reference AS Reference,
            mld.Name,
            mld.StartDate,
            mld.EndDate AS ExpiryDate,
            CASE
                WHEN mld.LicenceType = 'ClubPlus' THEN 'JustGo for Clubs'
                ELSE mld.LicenceType
            END AS LicenceType,
            pri.CurrentStateId,
            st.Name AS [State],
            ld.Benefits,
            ld.Licencetype AS LicenseCategory,
            -- Prefer LEFT JOIN for Club lookup instead of scalar subquery
            ISNULL(cd.clubName, 'NGB') AS OwnerName,
            CASE 
                WHEN ISNULL(ld.LicenceOwner, 0) = 0 THEN 'NGB' 
                ELSE 'Club' 
            END AS OwnerType
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
        ORDER BY mld.EndDate DESC;";
        }
    }
}
