using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting
{
    public class GetGlobalSettingQuery : IRequest<string>
    {
        public required string ItemKey { get; set; }
    }
}
