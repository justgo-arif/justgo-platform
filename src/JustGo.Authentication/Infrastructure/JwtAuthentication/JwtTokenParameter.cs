using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace JustGo.Authentication.Infrastructure.JwtAuthentication
{
    public class JwtTokenParameter
    {
        public string SecretKey { get; set; }
        public int ExpiryMinutes { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public Guid TenantGuid { get; set; }
        public string TenantClientId { get; set; }
        public string UserName { get; set; }
        public Guid UserSyncId { get; set; }
        //public DateOnly DateOfBirth { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public IList<string> Claims { get; set; }
        public IList<string> Groups { get; set; }
        public IList<string> Roles { get; set; }
        public IList<string> MetaRoles { get; set; }
        public IList<string> AbacRoles { get; set; }
        public IList<string> ClubsIn { get; set; }
        public IList<string> ClubsAdminOf { get; set; }
        public IList<string> ClubsAdminOfWithChild { get; set; }
        public IList<Membership> Memberships { get; set; }
        public IList<string> FamilyMembers { get; set; }
        //For Json Web Encryption
        public string EncryptionKey { get; set; }
        public string KeyEncryptionAlgorithm { get; set; } = SecurityAlgorithms.Aes256KW; // Key wrap algorithm
        public string ContentEncryptionAlgorithm { get; set; } = SecurityAlgorithms.Aes256CbcHmacSha512; // Content encryption algorithm
        public bool UseJweEncryption { get; set; } = true;
    }
}
