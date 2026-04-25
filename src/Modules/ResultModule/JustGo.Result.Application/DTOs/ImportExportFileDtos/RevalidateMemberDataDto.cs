namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class RevalidateMemberDataDto
    {
        public int UploadedMemberDataId { get; set; }
        public string MemberDataJson { get; set; } = string.Empty;
    }
}
