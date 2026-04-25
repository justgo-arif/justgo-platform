using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.AssetLeases;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases
{
    public class EditAssetLeaseCommand: IRequest<string>
    {
        public string AssetLeaseId { get; set; }
        public List<LeaseeDTO> LeaseOwners { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime LeaseEndDate { get; set; }
        public AssetLeaseDateRangeType DateRangeType { get; set; }
        public string Comments { get; set; }
        public List<AssetLeaseAttachmentDTO> LeaseAttachment { get; set; }
    }
}
