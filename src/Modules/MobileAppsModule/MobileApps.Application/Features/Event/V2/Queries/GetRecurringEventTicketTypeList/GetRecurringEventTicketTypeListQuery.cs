using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventTicketTypeList 
{
    public class GetRecurringEventTicketTypeListQuery : IRequest<List<Dictionary<string, object>>>
    {
        public long RowId { get; internal set; }
        public GetRecurringEventTicketTypeListQuery(long id) 
        {
            RowId = id;
        }
    }
}
