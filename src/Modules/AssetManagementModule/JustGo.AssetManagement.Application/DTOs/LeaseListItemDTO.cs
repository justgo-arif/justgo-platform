namespace JustGo.AssetManagement.Application.DTOs
{
    public class LeaseListItemDTO
    {

        public string AssetLeaseId { get; set; }
        public string AssetRegisterId { get; set; }
        public string AssetReference { get; set; }
        public string AssetName { get; set; }
        public bool LeasedIn { get; set; }
        public DateTime LeaseStartDate { get; set; }
        public DateTime? LeaseEndDate { get; set; }
        public List<AssetOwnerViewDTO> CounterParty { get; set; }
        public List<AssetImageDTO> AssetImages { get; set; }
        public string LeaseStatus { get; set; }

    }
}
