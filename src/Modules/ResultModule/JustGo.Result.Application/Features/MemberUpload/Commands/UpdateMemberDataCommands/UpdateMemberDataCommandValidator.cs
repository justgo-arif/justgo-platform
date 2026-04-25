using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.UpdateMemberDataCommands
{
    public class UpdateMemberDataCommandValidator : AbstractValidator<UpdateMemberDataCommand>
    {
        public UpdateMemberDataCommandValidator()
        {
            RuleFor(x => x.UpdateMemberDataDto)
                .NotNull().WithMessage("UpdateMemberDataDto is required.");

            When(x => true, () =>
            {
                RuleFor(x => x.UpdateMemberDataDto.Id)
                    .GreaterThan(0).WithMessage("Id must be greater than 0.");

                RuleFor(x => x.UpdateMemberDataDto.Updates)
                    .NotNull().WithMessage("Updates dictionary is required.");
            });
        }
    }
}
