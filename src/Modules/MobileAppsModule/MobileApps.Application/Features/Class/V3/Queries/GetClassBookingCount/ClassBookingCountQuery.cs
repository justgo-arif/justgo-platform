using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class ClassBookingCountQuery : IRequest<IList<IDictionary<string, object>>>
    {
        public Guid ClubSyncGuid { get; set; }
    }
}
