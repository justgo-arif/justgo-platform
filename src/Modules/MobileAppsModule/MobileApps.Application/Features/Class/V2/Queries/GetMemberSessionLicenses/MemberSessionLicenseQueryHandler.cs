using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Queries.GetMemberSessionLicenses
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
            string sql = @"select isnull(so.MembershipIds, '') as MembershipIds
                                    from JustGoBookingClassSessionOption so
                                        inner join JustGoBookingClassSession cs
                                            on so.SessionId = cs.SessionId
                                    where so.SessionId = @SessionId;";

            var queryParam = new DynamicParameters();
            queryParam.Add("SessionId", request.SessionId);


            var result = await _readRepository.Value.GetSingleAsync(sql, queryParam, null, "text");

            string licenceSql = @"SELECT 
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
            CASE 
                WHEN ISNULL(ld.LicenceOwner, 0) = 0 THEN 'NGB' 
                ELSE (
                    SELECT cd.clubName 
                    FROM clubs_default cd 
                    WHERE cd.docid = ISNULL(ld.LicenceOwner, 0)
                ) 
            END AS OwnerName,
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
            WHERE 
                ll.Entityparentid = 11 
                AND mld.Entityid = @MemberDocId 
                AND ld.DocId IN (SELECT value FROM STRING_SPLIT(@MembershipIds, ','))
            ORDER BY mld.EndDate DESC;";

            var queryParams = new DynamicParameters();
            queryParams.Add("MemberDocId", request.MemberDocId);
            queryParams.Add("MembershipIds",  result); 
            var resultLicence = await _readRepository.Value.GetListAsync(licenceSql, queryParams, null, "text");


            return JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(JsonConvert.SerializeObject(resultLicence));
        }
    }
}
