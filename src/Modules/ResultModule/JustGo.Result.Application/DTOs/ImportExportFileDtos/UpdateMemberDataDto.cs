namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class UpdateMemberDataDto
    {
        public int Id { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public Dictionary<string, string> Updates { get; set; } = new();
    }
}
