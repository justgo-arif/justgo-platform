using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.Common.Queries.GetLicenseStatuses
{
    public class GetCredentialStatusIdQuery : IRequest<int>
    {
        public CredentialStatusType Status { get; set; }
    }
}
