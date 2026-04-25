using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class LeaseHistoryItemDTO 
    {

        public string AssetLeaseId { get; set; }
        public string AssetRegisterId { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime? LeaseEndDate { get; set; }
        public string LeaseStatus { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOwnersAdmin { get; set; }
        public List<AssetLeaseOwnerGridViewDTO> Leasees { get; set; }


    }
}
