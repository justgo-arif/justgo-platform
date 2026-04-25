using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberCommands
{
    public class DeleteMemberCommandValidator : AbstractValidator<DeleteMemberCommand>
    {
        public DeleteMemberCommandValidator()
        {
            RuleFor(x => x.UploadedMemberIds)
                .NotNull().WithMessage("Id list must not be null.")
                .NotEmpty().WithMessage("Id list must not be empty.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Id list contains duplicates.")
                .Must(ids => ids.All(id => id > 0)).WithMessage("All Ids must be greater than zero.")
                .Must(ids => ids.Count <= 2000).WithMessage("Too many IDs provided. Please provide fewer than 2000 IDs.");
        }
    }
}
