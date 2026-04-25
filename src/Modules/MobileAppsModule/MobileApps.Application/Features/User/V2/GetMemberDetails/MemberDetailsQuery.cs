using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Members;

namespace MobileApps.Application.Features.User.V2.GetMemberDetails
{
    public class MemberDetailsQuery:IRequest<IDictionary<string, object>>
    {
        public int MemberDocId { get; set; }    
    }
}
