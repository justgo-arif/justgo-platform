using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Commands.CancelLicenses
{
    public class CancelLicenseCommand : IRequest<bool>
    {
        public string AssetLicenseId { get; set; }
        public LicenseCancelReason Reason { get; set; }
        public string Note { get; set; }
        public DateTime EffectiveFrom { get; set; }
    }
}
