using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.SystemSetting.Commands.UpdateSystemSettings
{
    public class SystemSettingCommand:IRequest<bool>
    {
        public required string ItemKey { get; set; }
        public required string Value { get; set; }
        public bool Restricted { get; set; } = false;
    }
}
