using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalances;

public class GetAdyenBalancesValidator : AbstractValidator<GetAdyenBalancesQuery>
{
    public GetAdyenBalancesValidator()
    {
        RuleFor(x => x.BalanceAccountId)
           .NotEmpty().WithMessage("BalanceAccountId is required.");
    }
}
