using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.DeleteFamilyMember
{
    public class DeleteFamilyMemberValidator : AbstractValidator<DeleteFamilyMemberCommand>
    {
        public DeleteFamilyMemberValidator()
        {
            RuleFor(r => r.FamilyDocId).NotEmpty().WithMessage("FamilyDocId is required.");
            RuleFor(r => r.MemberDocId).NotEmpty().WithMessage("MemberDocId is required.");
        }
    }
}
