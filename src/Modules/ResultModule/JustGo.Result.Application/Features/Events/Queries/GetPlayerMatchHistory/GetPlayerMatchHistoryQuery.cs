using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Authentication.Helper.Paginations.Offset;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerMatchHistory;

public class GetPlayerMatchHistoryQuery:PaginationParams ,IRequest<Result<PlayerMatchHistoryResponse>>
{
    [Required]
    public required string PlayerId { get; set; }
    public int? EventId { get; set; }
    public string? SearchTerm { get; set; }
    public string? ResultEventTypeId { get; set; } 
}