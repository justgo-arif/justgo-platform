

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetDTO 
    {
        public string AssetRegisterId { get; set; }
        public string CategoryId { get; set; }
        public string AssetReference { get; set; }
        public string AssetName { get; set; }
        public string AssetDescription { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public DateTime? ManufactureDate { get; set; }
        public string Brand { get; set; }
        public string SerialNo { get; set; }
        public string Group { get; set; }
        public decimal AssetValue { get; set; }
        public DateTime IssueDate { get; set; }
        public string AssetConfig { get; set; }
        public string Country { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string PostCode { get; set; }
        public string Category { get; set; }
        public string AssetStatus { get; set; }
        public int AssetDocCode { get; set; }
        public string Barcode { get; set; }
        public List<string> AssetTags { get; set; }
        public List<AssetImageDTO> AssetImages { get; set; }
        public List<AssetOwnerDetailViewDTO> AssetOwners { get; set; }
        public List<AssetCredentialDTO> AssetCredentials { get; set; }
        public List<AssetLicenseDTO> PrimaryLicenses { get; set; }
        public List<AssetLicenseDTO> AdditionalLicenses { get; set; }
    }
}
