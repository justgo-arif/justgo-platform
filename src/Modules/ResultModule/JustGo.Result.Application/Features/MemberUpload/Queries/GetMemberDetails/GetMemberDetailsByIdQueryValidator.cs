using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberDetails
{
    public class GetMemberDetailsByIdQueryValidator : AbstractValidator<GetMemberDetailsByIdQuery>
    {
        public GetMemberDetailsByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }
}