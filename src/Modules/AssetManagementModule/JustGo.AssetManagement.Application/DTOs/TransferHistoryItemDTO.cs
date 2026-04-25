using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class TransferHistoryItemDTO 
    {

        public string AssetTransferId { get; set; }
        public string AssetRegisterId { get; set; }
        public DateTime TransferDate { get; set; }
        public string TransferStatus { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOwnersAdmin { get; set; }
        public List<AssetTransferOwnerGridViewDTO> TransferToOwners { get; set; }
        public List<AssetTransferOwnerGridViewDTO> PreviousOwners { get; set; }

    }
}
