using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings
{
    public interface ISystemSettingsService
    {
        Task<string?> GetSystemSettingsByItemKey(string itemKey, CancellationToken cancellationToken);
        Task<string> GetSystemSettings(string key, CancellationToken cancellationToken, string entity = "", int entityId = -1);
        Task<List<Authentication.Infrastructure.SystemSettings.SystemSettings>> GetSystemSettingsByMultipleItemKey(string itemKeys, CancellationToken cancellationToken);
        Task<List<Authentication.Infrastructure.SystemSettings.SystemSettings>> GetSystemSettingsByMultipleItemKey(List<string> itemKeys, CancellationToken cancellationToken);
        Task<List<Authentication.Infrastructure.SystemSettings.SystemSettings>> GetSystemSettingsByMultipleItemKey(string[] itemKeys, CancellationToken cancellationToken);
    }
}
