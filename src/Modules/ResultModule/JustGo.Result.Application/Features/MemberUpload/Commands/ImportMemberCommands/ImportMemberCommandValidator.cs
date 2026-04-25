using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.ImportMemberCommands
{
    public class ImportMemberCommandValidator : AbstractValidator<ImportMemberCommand>
    {
        public ImportMemberCommandValidator()
        {
            RuleFor(x => x.FileDto)
                .NotNull().WithMessage("FileDto is required.");

            When(x => true, () =>
            {
                RuleFor(x => x.FileDto.FileId)
                    .NotNull().WithMessage("File id is required.");

            });
        }
    }
}
