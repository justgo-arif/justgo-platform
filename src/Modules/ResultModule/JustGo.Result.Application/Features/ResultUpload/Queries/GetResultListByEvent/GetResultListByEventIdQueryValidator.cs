using FluentValidation;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetResultListByEvent;

public class GetResultListByEventIdQueryValidator : AbstractValidator<GetResultListByEventIdQuery>
{
    private static readonly HashSet<string> AllowedSortByValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "FileName",
        "UploadedAt",
        "Disciplines"
    };


    private static readonly HashSet<string> AllowedOrderByValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "ASC",
        "DESC"
    };

    public GetResultListByEventIdQueryValidator()
    {
        RuleFor(x => x.EventId)
            .GreaterThan(0)
            .WithMessage("EventId must be greater than 0.")
            .WithErrorCode("INVALID_EVENT_ID");

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
    }
}