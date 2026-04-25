using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.SystemSetting.Commands.UpdateGlobalSettings
{
    public class GlobalSettingCommand : IRequest<bool>
    {
        public required string ItemKey { get; set; }
        public required string Value { get; set; }
        public bool IsEncrypted { get; set; } = false;
    }
}
