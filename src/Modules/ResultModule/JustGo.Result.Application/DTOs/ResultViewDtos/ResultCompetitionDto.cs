using JustGo.Authentication.Helper.Paginations.Keyset;

namespace JustGo.Result.Application.DTOs.ResultViewDtos;


public record ResultCompetitionDto
{
    public string ParticipantLabel { get; set; } = string.Empty;
    public string AssetsLabel { get; set; } = string.Empty;
    public bool ShowAssetsCount { get; set; }
    public required KeysetPagedResult<ResultCompetitions> Competitions { get; set; }
}

public record ResultCompetitions
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public int TotalParticipants { get; set; }
    public int TotalAssets { get; set; }
}