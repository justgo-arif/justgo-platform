using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace JustGo.Finance.Application.Features.Balances.Queries.GetAdyenPayouts
{
    public class GetAdyenPayoutsValidator : AbstractValidator<GetAdyenPayoutsQuery>
    {
        public GetAdyenPayoutsValidator()
        {
            RuleFor(x => x.MerchantId)
                .NotEmpty().WithMessage("MerchantId is required.");
        }
    }
}
