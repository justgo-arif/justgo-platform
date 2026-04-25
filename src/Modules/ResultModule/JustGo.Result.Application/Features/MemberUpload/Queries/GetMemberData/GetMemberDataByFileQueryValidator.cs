using FluentValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberData
{
    public class GetMemberDataByFileQueryValidator : AbstractValidator<GetMemberDataByFileQuery>
    {
        public GetMemberDataByFileQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0.");

            //RuleFor(x => x.OwnerId)
            //    .NotNull().WithMessage("OwnerId cannot be null.");

            RuleFor(x => x.FileId)
                .GreaterThan(0).WithMessage("FileId must be greater than 0.");

        }
    }
}
