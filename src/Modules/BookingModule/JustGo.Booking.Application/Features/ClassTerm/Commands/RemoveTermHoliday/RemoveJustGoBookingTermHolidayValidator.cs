using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.ClassTerm.Commands.RemoveTermHoliday
{
    public class RemoveJustGoBookingTermHolidayValidator : AbstractValidator<RemoveJustGoBookingTermHolidayCommand>
    {
        public RemoveJustGoBookingTermHolidayValidator()
        {
            RuleFor(x => x.TermHolidayId)
                .NotEmpty()
                .WithMessage("TermHolidayId is required")
                .GreaterThan(0)
                .WithMessage("TermHolidayId must be greater than 0");
        }
    }
}
