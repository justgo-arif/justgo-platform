using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInCurrentsBySyncGuid
{
    public class GetOptInCurrentsBySyncGuidQuery : IRequest<List<OptInCurrent>>
    {
        public GetOptInCurrentsBySyncGuidQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; set; }
    }
}
