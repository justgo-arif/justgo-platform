using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.AttendanceStatus
{
    public class GetAttendanceStatusListQuery : IRequest<List<Dictionary<string, object>>>
    {
    }
}
