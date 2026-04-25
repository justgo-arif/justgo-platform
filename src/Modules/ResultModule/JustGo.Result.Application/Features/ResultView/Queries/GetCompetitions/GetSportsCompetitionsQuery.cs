using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions;

public class GetSportsCompetitionsQuery : IRequest<Result<ResultCompetitionDto>>, IValidatableObject
{
    public GetSportsCompetitionsQuery(int eventId, SportType sportType)
    {
        EventId = eventId;
        SportType = sportType;
    }

    [JsonIgnore]
    public SportType SportType { get; set; }
    public int EventId { get; set; }
    
    [Required] public int PageNumber { get; set; } = 1;
    [Required] public int PageSize { get; set; } = 10;
    
    public string? Search { get; set; } = string.Empty;
    
    public int? FilterByDisciplineId { get; set; }
    
    public string? SortBy { get; set; } = "DisciplineName";
    
    public string? OrderBy { get; set; } = "ASC";
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validSortByFields = new List<string>
        {
            "DisciplineName",
            "CompetitionName"
        };

        if (!validSortByFields.Contains(SortBy ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                $"Invalid SortBy field. Valid fields are: {string.Join(", ", validSortByFields)}",
                [nameof(SortBy)]);
        }

        var validOrderByValues = new List<string> { "ASC", "DESC" };
        if (!validOrderByValues.Contains(OrderBy?.ToUpper() ?? string.Empty))
        {
            yield return new ValidationResult(
                "Invalid OrderBy value. Valid values are: ASC, DESC",
                [nameof(OrderBy)]);
        }

        if (PageNumber <= 0)
        {
            yield return new ValidationResult(
                "PageNumber must be greater than 0.",
                [nameof(PageNumber)]);
        }

        if (PageSize <= 0)
        {
            yield return new ValidationResult(
                "PageSize must be greater than 0.",
                [nameof(PageSize)]);
        }

        if (EventId <= 0)
        {
            yield return new ValidationResult(
                "EventId must be greater than 0.",
                [nameof(EventId)]);
        }

        if (FilterByDisciplineId is <= 0)
        {
            yield return new ValidationResult(
                "FilterByDisciplineId must be greater than 0 if provided.",
                [nameof(FilterByDisciplineId)]);
        }
    }
}