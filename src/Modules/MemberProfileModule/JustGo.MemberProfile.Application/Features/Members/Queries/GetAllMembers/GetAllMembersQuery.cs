using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetAllMembers
{
    public class GetAllMembersQuery : IRequest<List<MemberSummary>>
    {
        public int Userid { get; set; }
        public string LoginId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string EmailAddress { get; set; }
        public string ProfilePicURL { get; set; }
        public DateTime? DOB { get; set; }
        public string Gender { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set; }
        public DateTime? EmailVerified { get; set; }
        public string MemberId { get; set; }
        public Guid? UserSyncId { get; set; }
        public int SuspensionLevel { get; set; }
    }
}
