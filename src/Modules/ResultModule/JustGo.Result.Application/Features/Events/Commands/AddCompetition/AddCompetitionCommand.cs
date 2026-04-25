using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;
using System.ComponentModel.DataAnnotations;

namespace JustGo.Result.Application.Features.Events.Commands.AddCompetition;

public class AddCompetitionCommand : IRequest<Result<AddCompetitionResponse>>
{
    [Required]
    public string EventName { get; set; }= string.Empty;
    [Required]
    public DateTime StartDate { get; set; }= DateTime.MinValue;
    [Required]
    public DateTime EndDate { get; set; }= DateTime.MinValue;
    [Required]
    public string OwnerGuid { get; set; }= string.Empty;
    [Required]
    public int ResultEventTypeId { get; set; }
    public string Reference { get; set; }= string.Empty;
    public int TimeZone { get; set; }
    public int CategoryId { get; set; }
    public string ImagePath { get; set; }= string.Empty; 
    public string Postcode { get; set; }= string.Empty; 
    public string County { get; set; }= string.Empty; 
    public string Town { get; set; }= string.Empty; 
    public string Address1 { get; set; }= string.Empty; 
    public string Address2 { get; set; }= string.Empty;

}
public static class CompetitionSourceType
{
    public const string Manual = "Manual";
}
