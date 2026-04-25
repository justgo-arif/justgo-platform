using FluentValidation;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetLicenseDataCaptureItems
{
    public class GetLicenseDataCaptureItemsQueryValidator : AbstractValidator<GetLicenseDataCaptureItemsQuery>
    {
        public GetLicenseDataCaptureItemsQueryValidator()
        {
            RuleFor(x => x.LicenseDocId)
                .GreaterThan(0).WithMessage("LicenseDocId must be greater than zero.");
        }
    }
}
