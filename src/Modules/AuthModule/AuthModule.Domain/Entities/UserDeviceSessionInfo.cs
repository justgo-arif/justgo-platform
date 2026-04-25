using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class UserDeviceSessionInfo
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public Guid? UserSyncId { get; set; }
        public string UserSessionId { get; set; }
        public string UserDeviceName { get; set; }
        public string UserDeviceModel { get; set; }
        public string UserDeviceIP { get; set; }
        public string UserDevicePort { get; set; }
        public string UserBrowserName { get; set; }
        public string UserBrowserVersion { get; set; }
        public string RefreshToken { get; set; }
        public int RefreshTokenExpiryMinutes { get; set; }
        public DateTime RefreshTokenExpiryDate { get; set; }
        public string UserLocation { get; set; }
    }
}
