using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetSessionsDaysOfWeekList
{
    public class SessionsDaysOfWeekListQuery:IRequest<IEnumerable<Dictionary<string,object>>>
    {
        public int ClassId { get; set; }
    }
}
