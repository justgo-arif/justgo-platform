namespace JustGo.Membership.Application.DTOs
{
    public class MemberDetailsDto
    {
        public int Userid { get; set; }
        public int UserId { get; set; }
        public string Gender { get; set; }
        public int DocId { get; set; }
        public string MID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string DOB { get; set; }
        public string ProfilePicURL { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set; }
        public string Phone { get; set; }
        public int ActiveLicenseCount { get; set; }
        public string MemberSyncGuid { get; set; }
    }
}
