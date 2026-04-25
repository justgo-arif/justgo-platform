using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventCompetitions;

public class GetEventCompetitionsQuery : PaginationParams , IRequest<Result<EventCompetitionResponse>>
{
    [Required]
    public required int EventId { get; set; }
    public string? SearchTerm { get; set; }
    public string? ResultEventTypeId { get; set; }
}