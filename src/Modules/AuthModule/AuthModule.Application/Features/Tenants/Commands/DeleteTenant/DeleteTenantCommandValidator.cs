using FluentValidation;

namespace AuthModule.Application.Features.Tenants.Commands.DeleteTenant
{
    public class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
    {
        public DeleteTenantCommandValidator()
        {
            RuleFor(r => r.Id).NotEmpty().WithMessage("Id is required.")
                .GreaterThan(0).WithMessage("Id must be greater than zero.");
        }
    }
}
