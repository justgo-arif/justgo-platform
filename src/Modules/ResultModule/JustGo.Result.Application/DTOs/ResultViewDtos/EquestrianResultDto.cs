namespace JustGo.Result.Application.DTOs.ResultViewDtos;

public class EquestrianResultDto
{
    public string DisciplineName { get; set; } = string.Empty;
    public int Position { get; set; }
    public int CompetitionParticipantId { get; set; }
    public string MemberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetReference { get; set; } = string.Empty;
    public string AssetImage { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public string MemberImage { get; set; } = string.Empty;
    public string ResultsJson { get; set; } = string.Empty;
}
