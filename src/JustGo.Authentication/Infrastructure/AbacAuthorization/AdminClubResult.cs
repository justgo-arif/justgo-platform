using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class AdminClubResult
    {
        public string SyncGuid { get; set; } = string.Empty;
        public int DocId { get; set; }
        public string ClubName { get; set; } = string.Empty;
    }
}
