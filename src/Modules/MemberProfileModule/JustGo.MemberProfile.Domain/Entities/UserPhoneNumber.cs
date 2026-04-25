using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class UserPhoneNumber
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; }
        public string CountryCode { get; set; }
        public string Number { get; set; }
    }
}
