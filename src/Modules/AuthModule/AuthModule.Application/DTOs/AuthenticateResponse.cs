using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Application.DTOs
{
    public class AuthenticateResponse
    {
        public int Userid { get; set; }
        public string LoginId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public int? MemberDocId { get; set; }
        public string MemberId { get; set; }
        public Guid? UserSyncId { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
       
    }
}
