using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting
{
    public class DateTimeConversionQuery : IRequest<string>
    {
        public required DateTime EventDate { get; set; }
        public required int TimeZoneId { get; set; }    
    }
}
