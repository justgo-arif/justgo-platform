using JustGo.AssetManagement.Application.DTOs.AssetTransfers;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.OwnerTransferApprovalMetadata
{
    public class GetOwnerTransferApprovalMetadataQuery : IRequest<List<OwnerTransferApprovalMetadataDTO>>
    {
        public string TransferId { get; set; }
    }
}
