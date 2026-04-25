namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class MemberDetailsDto
    {
        public int Id { get; set; }
        public string MemberData { get; set; } = string.Empty;
        public bool IsHorseExists { get; set; } = true;
        public bool IsMemberExists { get; set; } = true;
        public string ValidationError { get; set; } = string.Empty;
    }
}
