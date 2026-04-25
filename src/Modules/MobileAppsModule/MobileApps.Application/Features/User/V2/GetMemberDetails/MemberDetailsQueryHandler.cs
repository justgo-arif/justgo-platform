using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Members;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.User.V2.GetMemberDetails
{
    class MemberDetailsQueryHandler : IRequestHandler<MemberDetailsQuery, IDictionary<string, object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public MemberDetailsQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IDictionary<string, object>> Handle(MemberDetailsQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT 
                -- Personal Info block
                (
                    SELECT 
                        u.Userid as UserId,
                        u.FirstName + ' ' + u.LastName AS FullName,
                        u.DOB AS DateOfBirth,
                         (u.Address1 + ISNULL('',' ' +u.Address2)) AS Address,
                        u.Gender,
                        u.MemberId,u.EmailAddress,
                        Contact=(select top 1 p.Number from UserPhoneNumber p where p.[Type]='Mobile' AND p.UserId=u.Userid)
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                ) AS PersonalInfo,

                -- Emergency Contact block (as an array)
                (
                    SELECT 
                        mc.FirstName + ' ' + mc.Surname AS FullName,
                        mc.ContactNumber,
                        mc.EmailAddress
                    FROM Members_EmergencyContact mc
                    WHERE mc.DocId = md.DocId
                    FOR JSON PATH
                ) AS EmergencyContacts

            FROM [User] u
            INNER JOIN Members_Default md ON u.MemberDocId = md.DocId
            WHERE u.MemberDocId =@MemberDocId";

            var queryParam = new DynamicParameters();
            queryParam.Add("MemberDocId", request.MemberDocId);

            var result = await _readRepository.Value.GetAsync(sql, queryParam, null, "text");

            return JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(result));
        }
    }
}
