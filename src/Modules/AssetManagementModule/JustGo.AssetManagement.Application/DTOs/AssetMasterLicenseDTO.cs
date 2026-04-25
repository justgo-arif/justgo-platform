using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetMasterLicenseDTO
    {
        public int LicenseDocId { get; set; }
        public int ProductDocId { get; set; }
        public string Location { get; set; }
        //public int LicenseDocId { get; set; }
        public Guid LicenseId { get; set; } // From d.SyncGuid AS Id
        public Guid ProductId { get; set; }
        public string Reference { get; set; }
        public string Benefits { get; set; }
        public string Description { get; set; }
        public decimal? HidePrice { get; set; }
        public decimal Sequence { get; set; }
        public string LicenceOwner { get; set; }
        public string Code { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public decimal Availablequantity { get; set; }
        public string ProductColor { get; set; }
        public string LicenseType { get; set; } // This maps RenewalWindow to LicenseType
        public string ExpiryDateEndingUnit { get; set; }
        public string ExpiryDateEndingValue { get; set; }
        public bool HideMembershipDuration { get; set; }
        public bool HideViewMoreAboutMembership { get; set; }
        //public string Classification { get; set; }
        public decimal PriceOption { get; set; }
        public decimal FromPrice { get; set; }
        public decimal ToPrice { get; set; }
        public string MembershipJourney { get; set; }
        public string AlternateDisplayCurrency { get; set; }
        public bool IsSubscriptionEnabled { get; set; }
        public string Recurringdescription { get; set; }
        public bool RecurringMandatory { get; set; }
        public int UpgradeType { get; set; }
        public int UpgradeId { get; set; }
        public string Expirydatestartingtype { get; set; }
        public string Expirydatestartingvalue { get; set; }
        public string LicenseConfig { get; set; }
    }
}
