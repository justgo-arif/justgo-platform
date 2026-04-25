using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V3.Queries
{
    public class SessionsDaysOfWeekListQuery:IRequest<IEnumerable<object>>
    {
        public required int ClubDocId { get; set; }
    }
}
