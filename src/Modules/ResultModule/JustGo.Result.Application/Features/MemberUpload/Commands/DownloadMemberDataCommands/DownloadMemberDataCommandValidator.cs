using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DownloadMemberDataCommands
{
    public class DownloadMemberDataCommandValidator : AbstractValidator<DownloadMemberDataCommand>
    {
        public DownloadMemberDataCommandValidator()
        {
            RuleFor(x => x.FileId)
                .GreaterThan(0)
                .WithMessage("FileId must be greater than 0.");
        }
    }
}