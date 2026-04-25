using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInMasterByOwner
{
    public class GetOptInMasterByOwnerQuery : IRequest<OptInMaster>
    {
        public GetOptInMasterByOwnerQuery(Guid syncGuid, string ownerType, int ownerId)
        {
            SyncGuid = syncGuid;
            OwnerType = ownerType;
            OwnerId = ownerId;
        }

        public Guid SyncGuid { get; set; }
        public string OwnerType { get; set; }
        public int OwnerId { get; set; }
    }
}
