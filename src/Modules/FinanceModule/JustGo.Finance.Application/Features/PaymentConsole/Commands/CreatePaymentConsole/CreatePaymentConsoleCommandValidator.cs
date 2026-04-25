using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole
{
    public class CreatePaymentConsoleCommandValidator : AbstractValidator<CreatePaymentConsoleCommand>
    {
        public CreatePaymentConsoleCommandValidator()
        {
            RuleFor(x => x.Products)
                .NotEmpty().WithMessage("At least one product is required.")
                .Must(NoDuplicateDescriptions).WithMessage("Duplicate product descriptions are not allowed.");
             
            RuleForEach(x => x.Products).SetValidator(new PaymentConsoleProductValidator());
        }

        private static bool NoDuplicateDescriptions(List<PaymentConsoleProduct> products)
        {
            if (products is null) return true;  

            var descriptions = products.Select(p => p.Description?.Trim().ToLower()).Where(d => d != null);
            return descriptions.Distinct().Count() == descriptions.Count();
        }
    }

}
