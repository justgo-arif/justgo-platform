using FluentValidation;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetClasses
{
    public class GetClassesBySyncGuidQueryValidator : AbstractValidator<GetClassesBySyncGuidQuery>
    {
        public GetClassesBySyncGuidQueryValidator()
        {
            RuleFor(x => x.OwningEntityGuid).NotNull().NotEmpty().WithMessage("Club guid is required.");

            RuleFor(x => x)
            .Must(x => x.CategoryGuid.HasValue || x.AgeGroupId.HasValue)
            .WithMessage("Either CategoryGuid or AgeGroupId must be provided.");

            RuleForEach(x => x.Days).Must(BeAValidDay).WithMessage("'{PropertyValue}' is not a valid day.");

            RuleFor(x => x.SortBy)
            .Must(BeAValidSort)
            .WithMessage("'{PropertyValue}' is not a valid sort value. Valid values are: Day, Class Group, Colour, Discipline, Age Group.");

            RuleFor(x => x.OrderBy)
            .Must(BeAValidOrder)
            .WithMessage("'{PropertyValue}' is not a valid order value. Valid values are: Asc, Desc.");
        }

        private static bool BeAValidDay(string day)
        {
            var validDays = new[] { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };
            return validDays.Contains(day, StringComparer.OrdinalIgnoreCase);
        }

        private static bool BeAValidSort(string sort)
        {
            var validSorts = new[] { "day", "class group", "colour", "discipline", "age group" };
            return !string.IsNullOrWhiteSpace(sort) && validSorts.Contains(sort.ToLower(), StringComparer.OrdinalIgnoreCase);
        }

        private static bool BeAValidOrder(string order)
        {
            var validOrders = new[] { "asc", "desc" };
            return !string.IsNullOrWhiteSpace(order) && validOrders.Contains(order.ToLower(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
