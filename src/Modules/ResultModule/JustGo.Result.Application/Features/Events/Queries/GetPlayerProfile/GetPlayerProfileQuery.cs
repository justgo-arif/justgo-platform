using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.Events.Queries.GetPlayerProfile;

public class GetPlayerProfileQuery : IRequest<Result<PlayerProfileDto>>
{
    [Required]
    public required string PlayerId { get; set; }
    public string? ResultEventTypeId { get; set; } 
}