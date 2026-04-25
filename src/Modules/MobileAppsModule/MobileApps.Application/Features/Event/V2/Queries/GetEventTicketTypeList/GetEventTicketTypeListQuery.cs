using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventTicketTypeList
{
    public class GetEventTicketTypeListQuery : IRequest<List<Dictionary<string, object>>>
    {
        public long EventDocId { get; internal set; }
        public GetEventTicketTypeListQuery(long docId)
        {
            EventDocId = docId;
        }
    }
}
