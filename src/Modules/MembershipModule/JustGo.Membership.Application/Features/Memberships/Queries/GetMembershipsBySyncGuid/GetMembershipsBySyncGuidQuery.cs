using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Membership.Domain.Entities;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipsBySyncGuid
{
    public class GetMembershipsBySyncGuidQuery : IRequest<List<Domain.Entities.Membership>>
    {
        public GetMembershipsBySyncGuidQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; set; }
    }
}
