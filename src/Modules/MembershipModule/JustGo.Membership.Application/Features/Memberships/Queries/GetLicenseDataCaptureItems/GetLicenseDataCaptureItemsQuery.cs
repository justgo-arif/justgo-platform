using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetLicenseDataCaptureItems
{
    public class GetLicenseDataCaptureItemsQuery : IRequest<List<LicenseDataCaptureItemDto>>
    {
        public int LicenseDocId { get; set; }
        public GetLicenseDataCaptureItemsQuery(int licenseDocId)
        {
            LicenseDocId = licenseDocId;
        }
    }
}
