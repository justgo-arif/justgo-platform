using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetLeases.Queries.OwnerLeaseApprovalMetadata
{
    public class GetOwnerLeaseApprovalMetadataQuery : IRequest<List<OwnerLeaseApprovalMetadataDTO>>
    {
        public string LeaseId { get; set; }
    }
}
