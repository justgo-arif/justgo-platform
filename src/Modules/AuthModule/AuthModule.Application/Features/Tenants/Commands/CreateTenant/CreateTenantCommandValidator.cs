using AuthModule.Application.Features.Tenants.Commands.CreateTenant;
using FluentValidation;

namespace AuthModule.Application.Features.Tenants.Commands.CreateHierarchyType
{
    public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
    {
        public CreateTenantCommandValidator()
        {
            RuleFor(r => r.TenantName).NotEmpty().WithMessage("TenantName is required.");
        }
    }
}
