using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Membership.Domain.Entities;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipDownloadLinks
{
    public class GetMembershipDownloadLinksQuery : IRequest<MembershipDownloadLinks?>
    {
        public GetMembershipDownloadLinksQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; }
    }
}