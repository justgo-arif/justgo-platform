using FluentValidation;

namespace JustGo.Organisation.Application.Features.Organizations.Queries.GetClubs;

public class GetClubsQueryValidator : AbstractValidator<GetClubsQuery>
{
    public GetClubsQueryValidator()
    {
        RuleFor(x => x.UserSyncId).NotNull().NotEmpty().WithMessage("User Id is required.");

        RuleFor(x => x.SortBy)
        .Must(BeAValidSort)
        .WithMessage("'{PropertyValue}' is not a valid sort value. Valid values are: Name, Distance.");

        RuleFor(x => x.OrderBy)
        .Must(BeAValidOrder)
        .WithMessage("'{PropertyValue}' is not a valid order value. Valid values are: Asc, Desc.");
    }

    private static bool BeAValidSort(string sort)
    {
        var validSorts = new[] { "name", "distance" };
        return !string.IsNullOrWhiteSpace(sort) && validSorts.Contains(sort.ToLower(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeAValidOrder(string order)
    {
        var validOrders = new[] { "asc", "desc" };
        return !string.IsNullOrWhiteSpace(order) && validOrders.Contains(order.ToLower(), StringComparer.OrdinalIgnoreCase);
    }
}