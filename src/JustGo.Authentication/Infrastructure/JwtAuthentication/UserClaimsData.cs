using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public class UserClaimsData
    {
        public dynamic? User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> AbacRoles { get; set; } = new List<string>();
        public List<string> ClubsIn { get; set; } = new List<string>();
        public List<string> ClubsAdminOf { get; set; } = new List<string>();
        public List<string> FamilyMembers { get; set; } = new List<string>();
    }
}
