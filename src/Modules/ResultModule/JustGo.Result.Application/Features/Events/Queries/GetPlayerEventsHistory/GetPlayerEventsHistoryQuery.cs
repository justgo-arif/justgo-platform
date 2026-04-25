using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerEventsHistory;

public class GetPlayerEventsHistoryQuery : PaginationParams, IRequest<Result<PlayerEventsHistoryResponse>>
{
    [Required]
    public required string PlayerId { get; set; }
    public string? SearchTerm { get; set; }
    public string? ResultEventTypeId { get; set; } 
    public string? OwnerGuid { get; set; } 
    public string? Year { get; set; } 
}