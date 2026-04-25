namespace JustGo.Membership.Application.DTOs
{
    public class GroupDto
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Tag { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

}
