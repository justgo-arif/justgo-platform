using FluentValidation;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetEvents;


public class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    private static readonly HashSet<string> AllowedFilterByValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "All",
        "Draft", 
        "Published",
        "NoResult"
    };
    
    private static readonly HashSet<string> AllowedSortByValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "EventName",
        "StartDate", 
        "EndDate",
        "DisciplineName",
        "ResultProgress",
        "EventReference"
    };


    private static readonly HashSet<string> AllowedOrderByValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASC",
        "DESC"
    };

    public GetEventsQueryValidator()
    {
        RuleFor(x => x.FilterBy)
            .NotEmpty()
            .WithMessage("FilterBy parameter is required.")
            .Must(filterBy => AllowedFilterByValues.Contains(filterBy?.Trim() ?? string.Empty))
            .WithMessage($"Invalid FilterBy value. Allowed values are: {string.Join(", ", AllowedFilterByValues)}")
            .WithErrorCode("INVALID_FILTER_BY");

        RuleFor(x => x.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) || AllowedSortByValues.Contains(sortBy.Trim()))
            .WithMessage($"Invalid SortBy value. Allowed values are: {string.Join(", ", AllowedSortByValues)}")
            .WithErrorCode("INVALID_SORT_BY");

        RuleFor(x => x.OrderBy)
            .Must(orderBy => string.IsNullOrWhiteSpace(orderBy) || AllowedOrderByValues.Contains(orderBy.Trim()))
            .WithMessage($"Invalid OrderBy value. Allowed values are: {string.Join(", ", AllowedOrderByValues)}")
            .WithErrorCode("INVALID_ORDER_BY");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than 0.")
            .WithErrorCode("INVALID_PAGE_NUMBER");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than 0.")
            .LessThanOrEqualTo(100)
            .WithMessage("PageSize cannot exceed 100 to ensure optimal performance.")
            .WithErrorCode("INVALID_PAGE_SIZE");

        RuleFor(x => x.Search)
            .MaximumLength(255)
            .WithMessage("Search parameter cannot exceed 255 characters.")
            .WithErrorCode("INVALID_SEARCH_LENGTH")
            .When(x => !string.IsNullOrEmpty(x.Search));
    }
}