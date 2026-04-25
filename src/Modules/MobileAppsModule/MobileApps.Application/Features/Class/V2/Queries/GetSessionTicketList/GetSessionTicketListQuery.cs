using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionTicketList
{
    public class GetSessionTicketListQuery:IRequest<IList<IDictionary<string,object>>>
    {
        public int SessionId { get; set; }  
    }
}
