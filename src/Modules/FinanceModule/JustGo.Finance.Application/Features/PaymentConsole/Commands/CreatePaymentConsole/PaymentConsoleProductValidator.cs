using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
namespace JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole
{

    public class PaymentConsoleProductValidator : AbstractValidator<PaymentConsoleProduct>
    {
        public PaymentConsoleProductValidator()
        {
            RuleFor(p => p.CategoryId)
                .NotEmpty().WithMessage("CategoryId is required.");

            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(p => p.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");
        } 
    }

}
