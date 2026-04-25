using System.ComponentModel.DataAnnotations;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Commands.UpdateCompetition;

public class UpdateCompetitionCommand : IRequest<Result<UpdateCompetitionResponse>>
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; } = DateTime.MinValue;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.MinValue;

    [Required]
    public int ResultEventTypeId { get; set; }

    public int TimeZone { get; set; }
    public int CategoryId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string Town { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
}