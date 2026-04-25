namespace AuthModule.Domain.Entities
{
    public class User
    {
        public int Userid { get; set; }
        public string LoginId { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string Fax { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastPasswordUpdateDate { get; set; }
        public int? FailedLoginAttempt { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string Comments { get; set; }
        public DateTime? LastEditDate { get; set; }
        public string EmailAddress { get; set; }
        public bool ForceResetPassword { get; set; }
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
        public string Currency { get; set; }
        public int? MemberDocId { get; set; }
        public string ParentFirstname { get; set; }
        public string ParentLastname { get; set; }
        public string ParentEmailAddress { get; set; }
        public DateTime? ParentEmailVerified { get; set; }
        public DateTime? EmailVerified { get; set; }
        public int? ParentalOverrideUser { get; set; }
        public DateTime? ParentalOverrideDate { get; set; }
        public int? SourceUserId { get; set; }
        public string SourceLocation { get; set; }
        public string OtherGender { get; set; }
        public string MemberId { get; set; }
        public int? CountryId { get; set; }
        public int? CountyId { get; set; }
        public int SuspensionLevel { get; set; }
        public Guid? UserSyncId { get; set; }
    }
}