using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMemberMemberships
{
    public class GetMemberMembershipsQuery : IRequest<List<MembersHierarchiesWithMemberships>>
    {
        public GetMemberMembershipsQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; }
    }
}