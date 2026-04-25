using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class MemberDTO
    {
        public string MID { get; set; }
        public int Id { get; set; }
        public int MemberDocId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserId { get; set; }
        public string Image { get; set; }
        public string Gender { get; set; }
        public string EmailAddress { get; set; }
    }
    public class MemberWithCountTO : MemberDTO
    {
        public int TotalRows { get; set; }
    }

}
