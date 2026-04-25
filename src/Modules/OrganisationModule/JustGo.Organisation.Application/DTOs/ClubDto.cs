namespace JustGo.Organisation.Application.DTOs;

public class ClubDto
{
    public int ClubDocId { get; set; }
    public required string SyncGuid { get; set; }
    public required string ClubName { get; set; }
    public string? ClubImage { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Town { get; set; }
    public string? Postcode { get; set; }
    public string? County { get; set; }
    public string? Country { get; set; }
    public string? Lat { get; set; }
    public string? Lng { get; set; }
    public int RowNumber { get; set; }
    public int TotalRows { get; set; }
    public bool IsJoined { get; set; }
    public decimal Distance { get; set; }
    public string? ClubImagePath => string.IsNullOrWhiteSpace(ClubImage) ? null : "/store/download?f=" + ClubImage + "&t=repo&p=" + ClubDocId + "&p1=&p2=2";
}
