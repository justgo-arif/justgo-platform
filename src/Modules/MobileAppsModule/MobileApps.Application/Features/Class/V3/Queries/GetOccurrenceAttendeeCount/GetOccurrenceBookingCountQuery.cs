using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class GetOccurrenceBookingCountQuery:IRequest<IList<IDictionary<string, object>>>
    {
        public required int OccurrenceId { get; set; }
    
    }
}
