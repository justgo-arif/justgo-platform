namespace JustGo.MemberProfile.Domain.Entities
{
    public class MembersFamily
    {

        public int? MemberDocId { get; set; }
        public int Userid { get; set; }

        public Guid? UserSyncId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string MembershipName { get; set; } = string.Empty;

        public string Familyname { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;

        public DateTime ExpireDate { get; set; }
        public string ClubId { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public int FamilyDocId { get; set; }
        public string StateName { get; set; } = string.Empty;


    }
}
