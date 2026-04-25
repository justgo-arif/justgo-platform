using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Organisation.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubs;

public class GetClubsQuery : IRequest<KeysetPagedResult<ClubDto>>
{
    public required Guid UserSyncId { get; set; }
    public string[] Regions { get; set; } = [];
    public string[] ClubTypes { get; set; } = [];
    public required string SortBy { get; set; }
    public required string OrderBy { get; set; }
    public int? LastSeenId { get; set; }
    public int? TotalRows { get; set; }
    public int NumberOfRow { get; set; }
    public int Distance { get; set; }
    public string? Lat { get; set; }
    public string? Lng { get; set; }
    public string? KeySearch { get; set; }
}
