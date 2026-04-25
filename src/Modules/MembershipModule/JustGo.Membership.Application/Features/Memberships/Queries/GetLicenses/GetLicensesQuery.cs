using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetLicenses
{
    public class GetLicensesQuery : IRequest<List<MemberLicenseDto>>
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public int LicenseTypeField { get; set; }

        public GetLicensesQuery(Guid id, string type, int licenseTypeField)
        {
            Id = id;
            Type = type;
            LicenseTypeField = licenseTypeField;
        }
    }
}
