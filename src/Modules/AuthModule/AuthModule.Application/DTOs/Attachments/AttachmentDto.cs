using JustGo.Authentication.Infrastructure.Files;

namespace AuthModule.Application.DTOs.Attachments;

public class AttachmentDto
{
    public Guid AttachmentGuid { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public string UserName { get; set; }
    public string ProfilePicURL { get; set; }
    public DateTime CreatedDate { get; set; }
    public string FileSize => FileSizeHelper.ToPrettySize(Size, 2);
}
