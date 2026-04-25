using Newtonsoft.Json;

namespace JustGo.Result.Application.DTOs.UploadResultDtos;

using System.ComponentModel.DataAnnotations;

public class TableTennisResultDto
{
    public WinnerPlayerDto WinnerDto { get; set; } = new();

    public LoserPlayerDto LoserDto { get; set; } = new();

    public string ModifiedMemberData { get; set; } = string.Empty;
}


public abstract class BasePlayerInfoDto
{
    public abstract int MemberId { get; set; }
    
    public string? Name { get; set; }
    
    public string? ProfilePicUrl { get; set; }
    
    public string? Mobile { get; set; }
    
    public string? EmailAddress { get; set; }
    public string? Memberships { get; set; }
    
    public int MembershipCount { get; set; }
    public string? MembershipsExpiresOn { get; set; } = string.Empty;
}

public class WinnerPlayerDto : BasePlayerInfoDto
{
    [JsonProperty("Winner")]
    public override int MemberId { get; set; }
}

public class LoserPlayerDto : BasePlayerInfoDto
{
    [JsonProperty("Loser")]
    public override int MemberId { get; set; }
}