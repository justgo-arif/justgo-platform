using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMultipleOccurrenceBookingCount
{
    public class MultipleOccurrenceBookingCountQuery: IRequest<IList<IDictionary<string, object>>>
    {
        public required List<int> OccurrenceIds { get; set; }
    }
}
