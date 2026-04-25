namespace JustGo.MemberProfile.Application.DTOs;

public class SelectModelDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public string? Description { get; set; }
}
