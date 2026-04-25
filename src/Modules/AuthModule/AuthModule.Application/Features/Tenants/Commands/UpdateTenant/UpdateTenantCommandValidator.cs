using AuthModule.Application.Features.Tenants.Commands.UpdateTenant;
using FluentValidation;

namespace AuthModule.Application.Features.Tenants.Commands.UpdateTenant
{
    public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
    {
        public UpdateTenantCommandValidator()
        {
            RuleFor(r => r.Id).NotEmpty().WithMessage("Id is required.")
               .GreaterThan(0).WithMessage("Id must be greater than zero.");
            RuleFor(r => r.TenantName).NotEmpty().WithMessage("Tenant Name is required.");
        }
    }
}
