using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.BookingTransferRequest.Queries.CheckMemberPlanStatus
{
    public class CheckMemberPlanStatusValidator : AbstractValidator<CheckMemberPlanStatusQuery>
    {
        public CheckMemberPlanStatusValidator()
        {
            //RuleFor(x => x.sessionGuid)
            //    .GreaterThan(0)
            //    .WithMessage("SessionId must be greater than 0");

            RuleFor(x => x.MemberDocIds)
                .NotNull()
                .WithMessage("MemberDocIds cannot be null")
                .Must(list => list != null && list.Count > 0)
                .WithMessage("At least one MemberDocId must be provided")
                .Must(list => list != null && list.All(id => id > 0))
                .WithMessage("All MemberDocIds must be greater than 0");
        }
    }
}
