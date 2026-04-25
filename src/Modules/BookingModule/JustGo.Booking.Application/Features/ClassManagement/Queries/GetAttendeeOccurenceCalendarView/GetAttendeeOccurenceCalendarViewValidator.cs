using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.ClassManagement.Queries.GetAttendeeOccurenceCalendarView
{
    public class GetAttendeeOccurenceCalendarViewValidator : AbstractValidator<GetAttendeeOccurenceCalendarViewQuery>
    {
        public GetAttendeeOccurenceCalendarViewValidator()
        {
            RuleFor(x => x.CalendarRequest)
                .NotNull()
                .WithMessage("Calendar request payload is required.");

            When(x => x.CalendarRequest is not null, () =>
            {
                RuleFor(x => x.CalendarRequest.SessionGuid)
                    .NotEmpty()
                    .WithMessage("SessionGuid is required.");

                RuleFor(x => x.CalendarRequest.OwnerGuid)
                    .NotEmpty()
                    .WithMessage("OwnerGuid is required.");

                RuleFor(x => x.CalendarRequest.RowsPerPage)
                    .GreaterThan(0)
                    .WithMessage("RowsPerPage must be greater than 0.")
                    .LessThanOrEqualTo(100)
                    .WithMessage("RowsPerPage cannot exceed 100.");

                RuleFor(x => x.CalendarRequest.PageNumber)
                    .GreaterThan(0)
                    .WithMessage("PageNumber must be greater than 0.");

                RuleFor(x => x.CalendarRequest.FilterValue)
                    .MaximumLength(200)
                    .WithMessage("FilterValue cannot exceed 200 characters.")
                    .When(x => !string.IsNullOrWhiteSpace(x.CalendarRequest.FilterValue));
            });
        }
    }
}
