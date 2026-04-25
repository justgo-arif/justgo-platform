namespace JustGo.MemberProfile.Domain.Entities
{
    public class Family_Default
    {
        public int DocId { get; set; }
        public int RepositoryId { get; set; }
        public string RepositoryName { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public DateTime RegisterDate { get; set; }
        public string Location { get; set; }
        public bool IsLocked { get; set; }
        public int Status { get; set; }
        public string Tag { get; set; }
        public int Version { get; set; }
        public int UserId { get; set; }
        public string? Reference { get; set; }
        public string Familyname { get; set; }
    }
}
