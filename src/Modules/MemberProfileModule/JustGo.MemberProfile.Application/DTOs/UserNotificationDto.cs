namespace JustGo.MemberProfile.Application.DTOs;

public class UserNotificationDto
{
    public required string Message { get; set; }
    public required string Type { get; set; }
    public string? Param { get; set; }
}
