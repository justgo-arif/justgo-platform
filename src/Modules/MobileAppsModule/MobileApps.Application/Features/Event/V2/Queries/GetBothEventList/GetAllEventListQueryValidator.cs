using FluentValidation;

namespace MobileApps.Application.Features.Event.V2.Queries.GetBothEventList
{
    public class GetEventListQueryValidator : AbstractValidator<GetAllEventListQuery>
    {
        public GetEventListQueryValidator() 
        {
            RuleFor(r => r.StartDate)
             .Must(IsValid).WithMessage("StartDate must be a valid date.");

            RuleFor(r => r.EndDate)
            .Must(IsValid).WithMessage("EndDate must be a valid date.");

        }

        private bool IsValid(string? date)
        {
            if (date == "") return true;
            DateTime dateObj=DateTime.Parse(date);
            return dateObj!=null && dateObj > DateTime.MinValue;
        }
    }
}
