using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembersBasicDetailsQuery
{
    public class GetMembersBasicDetailsHandler : IRequestHandler<GetMembersBasicDetailsQuery, List<MemberDetailsDto>>
    {
        private readonly LazyService<IReadRepository<MemberDetailsDto>> _readRepository;

        public GetMembersBasicDetailsHandler(LazyService<IReadRepository<MemberDetailsDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<MemberDetailsDto>> Handle(GetMembersBasicDetailsQuery request, CancellationToken cancellationToken)
        {
            string sql = @"
            SELECT u.Userid, u.UserId, u.Gender, md.DocId, md.MID, u.FirstName, u.LastName, u.EmailAddress, u.DOB, u.ProfilePicURL,
                   u.Address1, u.Address2, u.Address3, u.Town, u.County, u.Country, u.PostCode,
                   (SELECT TOP(1) Number FROM UserPhoneNumber WHERE [Type] = 'Mobile' AND userid = u.UserId) AS Phone,
                   (SELECT COUNT(*) 
                    FROM Members_links ml
                    INNER JOIN MembersLicense_Default mld ON ml.Entityid = mld.DocId AND ml.Entityparentid = 21
                    INNER JOIN Processinfo pri ON pri.PrimaryDocId = mld.DocId AND pri.CurrentStateId = 62
                    WHERE ml.DocId = md.DocId) AS ActiveLicenseCount,
                   m_d.SyncGuid AS MemberSyncGuid
            FROM Members_Default md
            INNER JOIN document m_d ON md.DocId = m_d.DocId
            INNER JOIN EntityLink el ON el.LinkId = md.DocId
            INNER JOIN [User] u ON u.Userid = el.SourceId
            WHERE md.DocId IN @MemberDocIds";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberDocIds", request.MemberDocIds);

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
            return result;
        }
    }
}
