using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Domain.Entities
{
    public class UserMembership
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int StatusId { get; set; }
        public int MemberLicenseDocId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PaymentId { get; set; }
        public int LicenceOwner { get; set; }
        public DateTime CreatedDate { get; set; }
        public string MembershipName { get; set; }
        public string OwnerName { get; set; }
        public string MembershipStatus { get; set; }
        public DateTime LastActionDate { get; set; }
    }
}
