namespace JustGo.MemberProfile.Domain.Entities
{
    public class UserEmergencyContact
    {
        public int Id { get; set; }
        public int UserId { get; set; } 
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Relation { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public bool IsPrimary { get; set; }
        public required string CountryCode { get; set; }
        public required string RecordGuid { get; set; }
    }
}
