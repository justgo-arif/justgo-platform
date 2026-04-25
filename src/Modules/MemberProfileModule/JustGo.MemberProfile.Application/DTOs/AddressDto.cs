namespace JustGo.MemberProfile.Application.DTOs;

public class AddressDto
{
    public int AddressId { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Town { get; set; }
    public string? PostCode { get; set; }
    public int PostCodeId { get; set; }
    public string? Country { get; set; }
    public int RankNumber { get; set; }
}
