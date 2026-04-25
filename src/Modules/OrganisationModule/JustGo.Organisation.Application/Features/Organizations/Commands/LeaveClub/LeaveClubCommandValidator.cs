using FluentValidation;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.LeaveClub;

public class LeaveClubCommandValidator : AbstractValidator<LeaveClubCommand>
{
    public LeaveClubCommandValidator(IWriteRepositoryFactory writeRepoFactory, IUtilityService utilityService)
    {
        RuleFor(c => c.ClubGuid)
            .NotEqual(Guid.Empty)
            .WithMessage("Club guid is required.");

        RuleFor(c => c.MemberGuid)
            .NotEqual(Guid.Empty)
            .WithMessage("Member guid is required.");

        RuleFor(c => c.Reason)
            .NotEmpty()
            .WithMessage("Reason is required.");

    }
}
