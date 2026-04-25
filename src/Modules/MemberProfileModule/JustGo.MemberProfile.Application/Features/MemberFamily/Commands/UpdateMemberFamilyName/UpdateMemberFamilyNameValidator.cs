using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.UpdateMemberFamilyName
{

    public class UpdateMemberFamilyNameValidator : AbstractValidator<UpdateMemberFamilyNameCommand>
    {
        public UpdateMemberFamilyNameValidator()
        {
            RuleFor(x => x.FamilySyncGuid).NotEmpty();
            RuleFor(x => x.FamilyName).NotEmpty();
        }
    }
}
